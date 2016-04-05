using System.ServiceModel;

namespace Gqqnbig.Lego
{
    [ServiceContract]
    public interface IPhoneService
    {
        /// <summary>
        /// 开始一个会话，指定传输序号和图像大小。
        /// 如果不调用此方法，服务端将以之前的序号和预设的图像格式解析数据。
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <remarks>
        /// 注意可以用[ServiceContract(SessionMode = SessionMode.Required)]。
        /// http://blog.csdn.net/tcjiaan/article/details/8281782
        /// </remarks>
        [OperationContract]
        void StartSession(long seq, int width, int height);


        /// <summary>
        /// 将图像发送到服务端。
        /// </summary>
        /// <param name="seq">序号</param>
        /// <param name="image"></param>
        [OperationContract(IsOneWay = true)]
        void SendImage(long seq, byte[] image);
        //[OperationContract]
        //void EndSendImage(IAsyncResult r);
    }
}
