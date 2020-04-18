namespace RecycleBin.Notes

[<CLIMutable>]
type Icon = {
   IconUrl : string
   MediaType : string
}

[<CLIMutable>]
type NoteOptions = {
   /// Application title
   Title : string
   /// Application copyright
   Copyright : string
   /// IconUrl
   Icon : Icon
   /// GitHub URL
   GitHub : string
   /// Twitter URL
   Twitter : string
   /// GitHub account
   Owner : string
   /// GitHub project
   Repository : string
   /// GitHub access token
   AccessToken : string
}
