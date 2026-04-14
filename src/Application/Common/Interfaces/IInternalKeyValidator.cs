namespace Application.Common.Interfaces;

public interface IInternalKeyValidator
{
    bool IsValid(string internalKey);
}
