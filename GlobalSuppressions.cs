using System.Diagnostics.CodeAnalysis;

// Suppress CA1050 for the entire project (all types)
[assembly: SuppressMessage(
    "Design",
    "CA1050:Declare types in namespaces",
    Justification = "Project intentionally uses types in the global namespace",
    Scope = "namespace",
    Target = "~N:")]