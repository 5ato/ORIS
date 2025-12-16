namespace Signal.Core.Protocol.NMTP.Attributes;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FieldAttribute : Attribute
{
    public int FieldId { get; }

    public FieldAttribute(int fieldId)
    {
        FieldId = fieldId;
    }
}
