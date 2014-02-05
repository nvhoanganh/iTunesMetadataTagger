using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.RegularExpressions;
using iTunesLib;
using MediaInfoNET;

namespace RemoveDeadFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            var regexs = new List<string>();
            var searchString = "";
            // Read Values from Args
            if (args.Length > 0)
            {
                int index = 0;
                foreach (string par in args)
                {
                    if (par.ToLower() == "-s" && args.Length >= index + 1)
                        searchString = args[index + 1];

                    if (par.ToLower().StartsWith("-regex") && args.Length >= index + 1)
                        regexs.Add(args[index + 1]);
                    index++;
                }
            }

            // iTunes classes
            var itunes = new iTunesAppClass();
            IITLibraryPlaylist mainLibrary = itunes.LibraryPlaylist;
            IITTrackCollection tracks;
            if (searchString != "")
                // Search for the file
                tracks = mainLibrary.Search(searchString, ITPlaylistSearchField.ITPlaylistSearchFieldAll);
            else
                tracks = mainLibrary.Tracks;
            if (tracks == null)
            {
                Console.WriteLine("Found {0} tracks", 0);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return;
            }
            if (tracks.Count == 0)
            {
                Console.WriteLine("Found {0} tracks", 0);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Found {0} tracks", tracks.Count);
            IITFileOrCDTrack currTrack;

            // working variables
            int numTracks = tracks.Count;
            int updatedFile = 0;
            int skippedFile = 0;
            int errorCount = 0;
            DateTime startTime = DateTime.Now;

            while (numTracks != 0)
            {
                // only work with files
                currTrack = tracks[numTracks] as IITFileOrCDTrack;

                // is this a file track?
                if (currTrack != null && currTrack.Kind == ITTrackKind.ITTrackKindFile)
                {
                    if (currTrack.Location != null && System.IO.File.Exists(currTrack.Location))
                    {
                        var myfile = new MediaFile(currTrack.Location);
                        var propertiesFromMeta = myfile.General.Properties;
                        var regexMatchResults = MatchBasedOnFileName(regexs, currTrack.Location);
                        // Merge
                        propertiesFromMeta = MergeMetaAndRegex(regexMatchResults, propertiesFromMeta);
                        //var myfile = new MediaFile(@"\\ReadyShare\USB_Storage\Mediaserver\Music\JAZZ & VOCAL\Amos Lee\Amos Lee\Amos_Lee_-_Amos_Lee_-_03_-_Arms_Of_A_Woman.wav");
                        if (searchString.ToLower() == "processed" || string.IsNullOrEmpty(currTrack.Comment) || (currTrack.Comment != null && currTrack.Comment.IndexOf("processed") < 0))
                        {
                            try
                            {
                                if (regexMatchResults.Count > 0)
                                {
                                    Console.WriteLine("Processing (with Regex) {1} : {0}", myfile.Name, numTracks);
                                }
                                else
                                {
                                    Console.WriteLine("Processing {1} : {0}", myfile.Name, numTracks);
                                }
                                
                                UpdateProperty(currTrack, propertiesFromMeta, regexMatchResults);
                                currTrack.Comment = "processed";
                                updatedFile++;
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Console.WriteLine("ERROR - Skipping   : {0}. Error was \r\n {1}", myfile.Name, ex.Message);
                            }
                        }
                        else
                        {
                            skippedFile++;
                            Console.WriteLine("Skipping {1} : {0}", myfile.Name, numTracks);
                        }


                    }
                }

                // progress to the next tack
                numTracks--;
            }
            Console.WriteLine("Total Files processed : {0}", updatedFile);
            Console.WriteLine("Total Files skipped : {0}", skippedFile);
            Console.WriteLine("Total Files skipped due to error : {0}", errorCount);
            Console.WriteLine("Total time taken : {0} mins", DateTime.Now.Subtract(startTime).TotalMinutes);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static void UpdateProperty(IITFileOrCDTrack track, Dictionary<string, string> propertiesFromMetaData, Dictionary<string, string> propertiesFromRegex)
        {
            // Look at the metadata stored in the file first
            foreach (var entry in propertiesFromMetaData)
            {
                if (!string.IsNullOrEmpty(entry.Value))
                {
                    int intV;
                    string stringV;
                    switch (entry.Key)
                    {
                        case "Album":
                            track.Album = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Album");
                            break;
                        case "Album/Performer":
                            track.AlbumArtist = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Album/Performer"); 
                            break;
                        case "Performer":
                            track.Artist = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Performer"); 
                            break;
                        case "Genre":
                            track.Genre = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Genre"); 
                            break;
                        case "Track name/Position":
                            stringV = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Track name/Position");
                            if (int.TryParse(stringV, out intV))
                                track.TrackNumber = int.Parse(stringV);
                            break;
                        case "Track name":
                            track.Name = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Track name"); 
                            break;
                        case "Composer":
                            track.Composer = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Composer"); 
                            break;
                        case "Grouping":
                            track.Grouping = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Grouping"); 
                            break;
                        case "Recorded date":
                            stringV = GetValueFromDictionary(entry.Value, propertiesFromRegex, "Recorded date");
                            if (stringV != "")
                            {
                                if (stringV.Trim().Length >= 4)
                                {
                                    var releaseDt = stringV.Trim().Substring(0, 4);
                                    if (int.TryParse(releaseDt, out intV))
                                        track.Year = intV;
                                }
                            }
                            break;
                    }
                }
            }
            if (string.IsNullOrEmpty(track.Artist))
                track.Artist = "Uknown Artist";
            if (string.IsNullOrEmpty(track.Album))
                track.Album = "Uknown Album";
            if (string.IsNullOrEmpty(track.Genre))
                track.Genre = "Uknown Genre";
        }

        private static Dictionary<string, string> MergeMetaAndRegex(Dictionary<string, string> regexDat,
            Dictionary<string, string> metaData)
        {
            foreach (var ele in regexDat)
            {
                if (!metaData.ContainsKey(ele.Key))
                {
                    metaData.Add(ele.Key,ele.Value);
                }
            }
            return metaData;
        }

        private static string GetValueFromDictionary(string metaValue, Dictionary<string, string> regexData, string key)
        {
            if (string.IsNullOrEmpty(metaValue) && regexData.ContainsKey(key))
            {
                return regexData["Genre"];
            }
            return metaValue;
        }

        private static Dictionary<string, string> MatchBasedOnFileName(List<string> regexs, string filename)
        {
            var toreturn = new Dictionary<string, string>();
            if (regexs.Count == 0) return toreturn;
            foreach (string regex in regexs)
            {
                var parse = new Regex(regex);
                if (parse.IsMatch(filename))
                {
                    var matches = parse.Match(filename);
                    if (!string.IsNullOrEmpty(matches.Groups["Genre"].Value))
                        toreturn.Add("Genre", matches.Groups["Genre"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["Performer"].Value))
                        toreturn.Add("Performer", matches.Groups["Performer"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["Album"].Value))
                        toreturn.Add("Album", matches.Groups["Album"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["TrackName"].Value))
                        toreturn.Add("Track name", matches.Groups["TrackName"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["Grouping"].Value))
                        toreturn.Add("Grouping", matches.Groups["Grouping"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["AlbumPerformer"].Value))
                        toreturn.Add("Album/Performer", matches.Groups["AlbumPerformer"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["Composer"].Value))
                        toreturn.Add("Composer", matches.Groups["Composer"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["TrackNumber"].Value))
                        toreturn.Add("Track name/Position", matches.Groups["TrackNumber"].Value);

                    if (!string.IsNullOrEmpty(matches.Groups["Recordeddate"].Value))
                        toreturn.Add("Recorded date", matches.Groups["Recordeddate"].Value);

                    // stop at the first match
                    return toreturn;
                }
            }
            return toreturn;
        }
    }
}
