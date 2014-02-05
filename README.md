iTunesMetadataTagger
====================

This command line application will automatically update iTunes track with missing metadata which is stored in the 
audio file (itunes doesn't read metadata for .WAV files, see https://discussions.apple.com/thread/3565668?tstart=0) . 

If there is no metadata stored in the files, you can provide regular expression to extract various 
information from the audio file path (such as Genre, Album, Artist, etc). Regular Exression knowledge 
is required. Some examples of regular expression are provided. 

Command Line:

iTunesMetaDataTagger [-s] [searchTerm] [-regex] [regex string]
	-s				search string, application will only process tracks that match this term (Optional)
	-regex				Regular Expression used to extract metadata from file path 


Examples:
========
	
1. iTunesMetaDataTagger.exe		
   	
	When No parameter is provided, App will go through all tracks (which were not processed), 
	and update metadata from the metadata stored in the audio files (for .WAV file)

2. iTunesMetaDataTagger.exe -s "R&B"	
	
	Search for all tracks that contains the word "R&B" and update metadata from file

3. iTunesMetaDataTagger.exe -s "R&B" -regex "\\Music\\(?<Genre>[^\\]+)\\(?<Performer>[^\\]+)\\(?<Album>[^\\]+)\\(?<TrackName>[^\\]+)\.[a-zA-Z0-9]{3,}$"
	
	Search for all tracks that contains the word "R&B" and update metadata from file. 
	If there is no metadata stored in the file and the file path matches the Regular expression,
	metadata will be taken from Regular Expression. (In this case, Genre, Artist, Album and TrackName 
	will be populated (Metadata stored in file will take precedent) 

4. iTunesMetaDataTagger.exe -s "R&B" -regex "\\Music\\(?<Genre>[^\\]+)\\(?<Performer>[^\\]+)\\(?<Album>[^\\]+)\\(?<TrackName>[^\\]+)\.[a-zA-Z0-9]{3,}$" -regex "\\Music\\(?<Genre>[^\\]+)\\(?<Album>[^\\]+)\\(?<TrackName>[^\\]+)\.[a-zA-Z0-9]{3,}$"
						
	Search for all tracks that contains the word "R&B" and update metadata from file. 
	If there is no metadata stored in the file and the file path matches the Regular expression,
	metadata will be taken from Regular Expression. When multiple Regular Expressions is provided
	the first matching expression will be used. (You need to place the more specific Regular Expression first
	then follow by more generic ones. You can specify as many Regular Expression as you like)

Notes:
========

1. iTunes must be installed and running when you run this application.
2. Everytime this application has successfully updated a track, it will set the comments field in iTunes to "processed". The next time, it will skip all tracks that comments field is "processed", unless you specifically set the search term to be "processed" (which is equivalent to reprocess all tracks again)
3. supported itunes version: from 11.1.3.x
4. .Net 3.5 or above is required. 
