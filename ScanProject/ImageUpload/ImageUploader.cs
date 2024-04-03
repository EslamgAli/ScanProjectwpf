using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ScanProject.ImageUpload
{
    public class ImageUploader
    {

        public async static Task<HttpResponseMessage> UploadImage(byte[] imageBytes, string imageName, string folderName)
        {
            using (HttpClient client = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                // Set the API endpoint URL
                string apiUrl = "http://localhost:5029/api/SaveImage";

                // Add the image data to the form data
                formData.Add(new ByteArrayContent(imageBytes), "image", imageName);
                //folderName
                formData.Add(new StringContent(folderName), "folderName");
                // Send the HTTP POST request to the API
                HttpResponseMessage response = await client.PostAsync(apiUrl, formData);

                return response;
            }
        }

        public static byte[] ConvertImageFromBitMapToBytes(BitmapSource bitmapSource)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Create a BitmapFrame from the BitmapSource
                BitmapFrame frame = BitmapFrame.Create(bitmapSource);

                // Create an encoder (PNG encoder in this case) to encode the BitmapFrame into a byte array
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);

                // Save the BitmapFrame to the memory stream
                encoder.Save(stream);

                // Return the byte array from the memory stream
                var imageBytes = stream.ToArray();
                return imageBytes;
            }
        }
    }
}
