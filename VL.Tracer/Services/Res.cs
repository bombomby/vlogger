using VL.Transport;

namespace VL.Tracer.Services
{
    public static class Res
    {
        public static Task<Result> Ok() { return Task.FromResult(new Result() { Code = Result.Types.ErrorCode.Ok, Message = String.Empty }); }
        public static Task<Result> Warning(String message) { return Task.FromResult(new Result() { Code = Result.Types.ErrorCode.Warning, Message = message }); }
        public static Task<Result> Error(String message) { return Task.FromResult(new Result() { Code = Result.Types.ErrorCode.Warning, Message = message }); }
    }
}
