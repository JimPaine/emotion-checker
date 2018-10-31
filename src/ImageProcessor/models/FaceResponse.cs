namespace ImageProcessor.models
{
    public class FaceResponse
    {
        public string faceId { get; set; }

        public FaceRectangle faceRectangle { get; set;}

        public FaceResponseDetail faceAttributes { get; set; }
    }
}