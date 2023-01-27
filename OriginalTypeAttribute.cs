using System;

namespace ConnectToUrl;

/// <summary>
///   This attribute has no runtime functionality. It only exists to keep track
///   of the original C and C++ types from external headers and documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
internal class OriginalTypeAttribute : Attribute {
    public OriginalTypeAttribute(String value) {}
}