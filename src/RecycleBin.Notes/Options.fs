namespace RecycleBin.Notes

[<CLIMutable>]
type NoteOptions = {
   /// Application title
   Title : string
   /// Application copyright
   Copyright : string
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
