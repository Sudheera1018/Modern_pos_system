using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ModernPosSystem.Helpers;

public static class TempDataExtensions
{
    public static void PutToast(this ITempDataDictionary tempData, string type, string message)
    {
        tempData["Toast.Type"] = type;
        tempData["Toast.Message"] = message;
    }
}
