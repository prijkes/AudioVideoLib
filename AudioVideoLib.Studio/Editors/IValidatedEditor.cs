namespace AudioVideoLib.Studio.Editors;

/// <summary>
/// Implemented by editor view-models that participate in OK-button validation.
/// </summary>
public interface IValidatedEditor
{
    bool Validate(out string? error);
}
