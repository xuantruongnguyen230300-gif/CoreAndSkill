using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Http;

// Ánh xạ ErrorType → HTTP status — đúng MỘT chỗ, KHÔNG reflection (luật R4/R5, be-api-controller.md §1).
public static class ResultToHttpMapper
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
    };
}
