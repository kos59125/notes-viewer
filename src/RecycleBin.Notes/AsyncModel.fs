namespace RecycleBin.Notes

type AsyncModel<'Model> =
   | Loading
   | Loaded of 'Model
   | LoadError of exn

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncModel =
   let map f = function
      | Loading -> Loading
      | Loaded(model) -> Loaded(f model)
      | LoadError(ex) -> LoadError(ex)
