## Martha Stewart TV Downloader

**What it does**
- Supports both collections of videos and individual videos.
- Downloads the episodes / videos with the title from the website.
- Downloads the subtitles (if they are available) in srt format.
- Downloads at the highest resolution available. 


**Other info**  
Downloads can be slow, but thats a server thing out of my control.

You need a valid subscription on the site for this to work. A trial will do.

There isnt anything I can do about the episode naming convention, I know it sucks.

If an error happens, the program will dump the error to error.txt, if u contact me about the error, makes sure you send me the contents of that file so i know whats going on. 

**Are you gonna steal my login info?** 
No, im not, its kept in a local file called credentials.json thats created at first run, the only use of the login info is to login to the website.

**Whats the 422 error that keeps showing up?**
Its an error thrown during login, I have no idea why it keeps happening, I spent far to long on trying to work out what the issue is to no avail.  I assume its something to do with the csrf token but i cant be sure.  But, even if it fails 3 or 4 times, it will eventually login and continue its business.  
If you know the issue let me know and we can try and fix it. 