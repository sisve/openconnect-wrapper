using System;

namespace ConnectToUrl;

/// <summary>
///   This attribute has no runtime functionality. It only exists to keep track
///   of the original C and C++ types from external headers and documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
internal class SourceTypeAttribute : Attribute {
    public SourceTypeAttribute(String value) {}
}

/// <summary>
///   This attribute has no runtime functionality. It only exists to keep track
///   of the original C and C++ types from external headers and documentation.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
internal class SourceReference : Attribute {
    public SourceReference(String filename, Int32 value) {}
    public SourceReference(String filename, Int32 start, Int32 end) {}
}

