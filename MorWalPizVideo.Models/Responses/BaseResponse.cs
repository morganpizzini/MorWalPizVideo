namespace MorWalPizVideo.Models.Responses
{
    public class BaseResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();

    }

    public class BaseResponse<T> : BaseResponse
    {
        //public BaseResponse()
        //{

        //}
        public BaseResponse(T data) : this(data,0)
        {
        }
        public BaseResponse(T data,int total) : this(data, total,string.Empty)
        {
        }
        public BaseResponse(T data,int total,string next)
        {
            Data = data;
            Next = next;
            if (total > 0) {
                Count = total;
                return;
            }
            if (data is System.Collections.ICollection enumVar)
            {
                Count = enumVar.Count;
            }
            else
            {
                Count = Data != null ? 1 : 0;
            }
        }
        public T? Data { get; private set; }
        public int Count { get; private set; }
        public string Next { get; private set; }
    }
}
