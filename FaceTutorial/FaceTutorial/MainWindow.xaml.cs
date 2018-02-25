using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Emgu.Util;
using Emgu.CV;

namespace FaceTutorial
{
    public partial class MainWindow : Window
    {
        // Replace the first parameter with your valid subscription key.
        //
        // Replace or verify the region in the second parameter.
        //
        // You must use the same region in your REST API call as you used to obtain your subscription keys.
        // For example, if you obtained your subscription keys from the westus region, replace
        // "westcentralus" in the URI below with "westus".
        //
        // NOTE: Free trial subscription keys are generated in the westcentralus region, so if you are using
        // a free trial subscription key, you should not need to change this region.
        private readonly IFaceServiceClient faceServiceClient =
            new FaceServiceClient("7b81304ca843487aa03a7cee7685b83e", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");

        Face[] faces;                   // The list of detected faces.
        String[] faceDescriptions;      // The list of descriptions for the detected faces.
        double resizeFactor;
        string startupPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;

        // The resize factor for the displayed image.
        public MainWindow()
        {
            Main();
            InitializeComponent();
            // Uploads the image file and calls Detect Faces.
        }
        int max_index = 0; //maybe just change this to 0 for demo
        bool use_frustration = true, use_tiredness = true, too_close = false;
        private void update_uses(bool frustration_on, bool tiredness_on) //user specifices which use to use
        {
            use_frustration = frustration_on; //specified value from user
            use_tiredness = tiredness_on;
        }
        private void take_pic()
        {
            VideoCapture capture = new VideoCapture(); //create a camera capture
            System.Drawing.Bitmap image = capture.QueryFrame().Bitmap; //take a picture
            image.Save(startupPath + "\\ApplicationData\\picture.jpg");
        }
        private async void Main()
        {
            Initialize_Queues();
            Stopwatch three_second_check = new Stopwatch();
            //update_uses(frstration_in, tiredness_in); //this needs to be the inputs from the front end side
            double size = 0;
            //perhaps call a picture
            take_pic();
            Stopwatch debug = new Stopwatch();
            while ((use_frustration || use_tiredness) && (debug.ElapsedMilliseconds < 10000))
            {
                three_second_check.Restart();
                //take picture and save the picture to a specific path
                take_pic();
                //file path needs to change, choose your own path
                //update_uses(frstration_in, tiredness_in); //this needs to be the inputs from the front end side
                string filePath = startupPath + "\\ApplicationData\\picture.jpg";
                faces = await UploadAndDetectFaces(filePath);

                while ((faces == null) && (three_second_check.ElapsedMilliseconds <= 3000))
                { }
                if ((faces == null) && (three_second_check.ElapsedMilliseconds >= 3000))
                {
                    too_close = true;
                    MessageBox.Show("No Face Detected");
                    break; //may or may not need to break
                }
                if (size <= 20)
                    size++;
                double max_area = -1;
                for (int i = 0; i < faces.Length; ++i) //find the closest face
                {
                    Face compare_face = faces[i];
                    if (compare_face.FaceRectangle.Width * compare_face.FaceRectangle.Height > max_area)
                    {
                        max_area = compare_face.FaceRectangle.Width * compare_face.FaceRectangle.Height;
                        max_index = i;
                    }
                }
                //MessageBox.Show(Eye_openning_ratio(faces[max_index]).ToString());
                Task.Delay(1000);
                try
                {
                    input_frame(faces[max_index], size);
                }
                catch (Exception e)
                {
                    //MessageBox.Show("No face detected");
                    continue;
                }
                //could do below with switch case instead
                if (Sleep(faces[max_index], use_frustration, use_tiredness) == 0)
                {
                    MessageBox.Show("Time to sleep, you are both tired and frustrated"); //this line should somehow tell the user to sleep (they are both)
                    break;
                }
                if (Sleep(faces[max_index], use_frustration, use_tiredness) == 1)
                {
                    MessageBox.Show("Time to sleep, you are tired"); //this line should somehow tell the user they are just tired
                    break;
                }
                if (Sleep(faces[max_index], use_frustration, use_tiredness) == 2)
                {
                    MessageBox.Show("Time to sleep, you are frustrated"); //this line should somehow tell the they are frustrated
                    break;
                }
                //MessageBox.Show("User does not need to sleep yet"); //this line should be eliminated
                while (three_second_check.ElapsedMilliseconds <= 3000) //waits to make sure three seconds has passed
                { }

            }
        }
        const double W1 = 1.2, W2 = (1.0 / 5.0); //weights for the ratios and for frustration
        const double TIRED_THRESHOLD = 0.175, FRUSTRATION_THRESHOLD = 1.0; //0.21 jes
        double average_ratio_tiredness = 0, average_frustration = 0; //average is going to determine whether or not to go to sleep
        Queue<double> ratio_queue = new Queue<double>(20); //stores the values for each frame
        Queue<double> frustration_queue = new Queue<double>(20); //stores frustration values
        private void Initialize_Queues()
        {
            ratio_queue.Clear();
            frustration_queue.Clear();
            for (int i = 0; i < 20; i++)
            {
                ratio_queue.Enqueue(0);
                frustration_queue.Enqueue(0);
            }
        }
        private void input_frame(Face face, double size)
        {

            double ratio_sum = W1 * Eye_openning_ratio(face) - W2 * yawn_ratio(face);
            //MessageBox.Show("Eye_openning_ratio: " + Eye_openning_ratio(face).ToString());
            //MessageBox.Show("\nYawn_Ratio: " + yawn_ratio(face).ToString());
            //MessageBox.Show("\nYawn_Ratio with multiplier: " + (W2 * yawn_ratio(face)).ToString());
            double current_sum_tiredness = 0, current_sum_frustration = 0;
            if (size != 21)
            {
                current_sum_tiredness = average_ratio_tiredness * (size - 1);
                average_ratio_tiredness = (current_sum_tiredness - ratio_queue.Dequeue() + ratio_sum) / size;
                //current_sum_frustration = average_frustration * (size - 1);
                //average_frustration = (current_sum_frustration - frustration_queue.Dequeue() + frustration(face)) / size;

                //average_frustration += (frustration_queue.Dequeue() / size);
                //average_frustration -= (frustration(face) / size);
                ratio_queue.Enqueue(ratio_sum);
                //frustration_queue.Enqueue(frustration(face));
            }
            else
            {
                current_sum_tiredness = average_ratio_tiredness * 20.0;
                average_ratio_tiredness = (current_sum_tiredness - ratio_queue.Dequeue() + ratio_sum) / 20.0;
                //current_sum_frustration = average_frustration * 20.0;
                //average_frustration = (current_sum_frustration - frustration_queue.Dequeue() + frustration(face)) / 20.0;

                //average_frustration += (frustration_queue.Dequeue() / 20.0);
                //average_frustration -= (frustration(face) / 20.0);
                ratio_queue.Enqueue(ratio_sum);
                //frustration_queue.Enqueue(frustration(face));
            }


        }

        private bool frustration(Face face)
        {
            double anger = 0, fear = 0;
            anger = face.FaceAttributes.Emotion.Anger * 100;
            fear = face.FaceAttributes.Emotion.Fear * 100;
            if (anger >= 10 || fear >= 10)
                return true;
            return false;
        }
        private int Sleep(Face face, bool use_frustration, bool use_tiredness)
        { //-1 means no need to sleep, 0 means user is tired and frustrated
          // 1 means user is tired, 2 means user is frustrated
          //MessageBox.Show(average_ratio_tiredness.ToString());
            if (average_ratio_tiredness <= TIRED_THRESHOLD && use_tiredness)
            {
                //if (average_frustration >= FRUSTRATION_THRESHOLD && use_frustration)
                if (frustration(face) && use_frustration)
                {
                    return 0; //means user is both
                }
                return 1; // means user is just tired
            }
            if (frustration(face) && use_frustration)
                return 2; // means user is just frustrated
            return -1; //means user is all good
        }
        // Displays the image and calls Detect Faces.

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the image file to scan from the user.
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            // Return if canceled.
            if (!(bool)result)
            {
                return;
            }

            // Display the image file.
            //string filePath = openDlg.FileName;
            string filePath = "C:\\Users\\johns\\Desktop\\Hack the Valley\\merge_from_ofoct.jpg";
            Uri fileUri = new Uri(filePath);
            //MessageBox.Show(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;

            // Detect any faces in the image.
            Title = "Detecting...";
            faces = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", faces.Length);

            if (faces.Length > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                resizeFactor = 96 / dpi;
                faceDescriptions = new String[faces.Length];
                double max_area = -1;
                for (int i = 0; i < faces.Length; ++i) //find the closest face
                {
                    Face compare_face = faces[i];
                    if (compare_face.FaceRectangle.Width * compare_face.FaceRectangle.Height > max_area)
                    {
                        max_area = compare_face.FaceRectangle.Width * compare_face.FaceRectangle.Height;
                        max_index = i;
                    }
                }

                Face face = faces[max_index];
                MessageBox.Show(face.FaceLandmarks.EyeRightTop.X.ToString());
                // Draw a rectangle on the face.
                drawingContext.DrawRectangle(
                    Brushes.Transparent,
                    new Pen(Brushes.Red, 2),
                    new Rect(
                        face.FaceRectangle.Left * resizeFactor,
                        face.FaceRectangle.Top * resizeFactor,
                        face.FaceRectangle.Width * resizeFactor,
                        face.FaceRectangle.Height * resizeFactor
                        )
                        );
                //draw a line for the coordinates
                drawingContext.DrawLine(
                    new Pen(Brushes.Red, 2),
                    new Point(face.FaceLandmarks.EyeRightTop.X * resizeFactor, face.FaceLandmarks.EyeRightTop.Y * resizeFactor),
                    new Point(face.FaceLandmarks.EyeRightBottom.X * resizeFactor, face.FaceLandmarks.EyeRightBottom.Y * resizeFactor)
                );
                drawingContext.DrawLine(
                    new Pen(Brushes.Red, 2),
                    new Point(face.FaceLandmarks.EyeLeftTop.X * resizeFactor, face.FaceLandmarks.EyeLeftTop.Y * resizeFactor),
                    new Point(face.FaceLandmarks.EyeLeftBottom.X * resizeFactor, face.FaceLandmarks.EyeLeftBottom.Y * resizeFactor)
                    );
                drawingContext.DrawLine(
                    new Pen(Brushes.Red, 2),
                    new Point(face.FaceLandmarks.UpperLipBottom.X * resizeFactor, face.FaceLandmarks.UpperLipBottom.Y * resizeFactor),
                    new Point(face.FaceLandmarks.UnderLipTop.X * resizeFactor, face.FaceLandmarks.UnderLipTop.Y * resizeFactor)
                    );
                // Store the face description.
                faceDescriptions[max_index] = ("Eye Openning Ratio: " + Eye_openning_ratio(face).ToString()
                    //   + "   , Average eye distance: " + average_eyelid_distance(face)
                    //   + "   , right eye distance: " + Right_eyelid_distance(face)
                    //   + "   , left eye distance: " + Left_eyelid_distance(face)
                    //   + "   , real constant distance: " + Real_constant_distance(face)
                    + "   , yawn raio: " + yawn_ratio(face)
                    + "   , frustration: " + frustration(face)
                    + "   , average: " + average_ratio_tiredness
                    //  + "   , pitch 'value': " + Math.Abs(face.FaceAttributes.HeadPose.Pitch).ToString()//can't use pitch as currently unsupported
                    ); //changed this from FaceDescription to eye function

                string message = "The current eye openning ratio: " + Eye_openning_ratio(faces[max_index]).ToString();
                message += "\nThe current lip open ratio is: " + yawn_ratio(faces[max_index]);//Should somehow have a large weighting on the yawn (maybe exponential)
                //message += "\nThe current pitch 'value' is: " + Math.Abs(faces[max_index].FaceAttributes.HeadPose.Pitch); //can't use pitch as currently unsupported
                MessageBox.Show(message);
                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;

                // Set the status bar text.
                faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
            }
        }
        // Uploads the image file and calls Detect Faces.

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Smile, FaceAttributeType.Emotion };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    Face[] faces = await faceServiceClient.DetectAsync(imageFileStream, returnFaceId: false, returnFaceLandmarks: true, returnFaceAttributes: faceAttributes);
                    return faces;
                }
            }
            // Catch and display Face API errors.
            catch (FaceAPIException f)
            {
                MessageBox.Show(f.ErrorMessage, f.ErrorCode);
                return new Face[0];
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return new Face[0];
            }
        }
        //below is own custom function, call the y_coordinate_eye_right_top instead of FaceDescription
        private double Right_eyelid_distance(Face face)
        {
            return Math.Sqrt(Math.Pow(face.FaceLandmarks.EyeRightTop.Y - face.FaceLandmarks.EyeRightBottom.Y, 2)
                        + Math.Pow(face.FaceLandmarks.EyeRightTop.X - face.FaceLandmarks.EyeRightBottom.X, 2));
        }
        private double Left_eyelid_distance(Face face)
        {
            return Math.Sqrt(Math.Pow(face.FaceLandmarks.EyeLeftTop.Y - face.FaceLandmarks.EyeLeftBottom.Y, 2)
                    + Math.Pow(face.FaceLandmarks.EyeLeftTop.X - face.FaceLandmarks.EyeLeftBottom.X, 2));
        }

        //below uses the fact that the distances are going to be the most constant values of the face
        //while also taking to account the paralax error introduced during tilt
        //this distance will be along the same axis as the eye distances (based off a diagram)
        private double Real_constant_distance(Face face)
        {
            double distance_right_side = 0, distance_left_side = 0, distance_nose_average = 0, distance_eyebrow_average = 0/*, distance_eyelid_horizontal_average = 0*/;
            distance_right_side = Math.Sqrt(Math.Pow(face.FaceLandmarks.NoseRootRight.Y - face.FaceLandmarks.NoseTip.Y, 2)
                    + Math.Pow(face.FaceLandmarks.NoseRootRight.X - face.FaceLandmarks.NoseTip.X, 2));
            distance_left_side = Math.Sqrt(Math.Pow(face.FaceLandmarks.NoseRootLeft.Y - face.FaceLandmarks.NoseTip.Y, 2)
                    + Math.Pow(face.FaceLandmarks.NoseRootLeft.X - face.FaceLandmarks.NoseTip.X, 2));
            distance_nose_average = (distance_left_side + distance_right_side) / 2.0;
            //return distance_average; //another method may be to measure from nose tip to upper lib
            distance_eyebrow_average = Math.Sqrt(Math.Pow(face.FaceLandmarks.EyebrowLeftInner.Y - face.FaceLandmarks.EyebrowLeftOuter.Y, 2)
                    + Math.Pow(face.FaceLandmarks.EyebrowLeftInner.X - face.FaceLandmarks.EyebrowLeftOuter.X, 2));
            distance_eyebrow_average += Math.Sqrt(Math.Pow(face.FaceLandmarks.EyebrowRightInner.Y - face.FaceLandmarks.EyebrowRightOuter.Y, 2)
                    + Math.Pow(face.FaceLandmarks.EyebrowRightInner.X - face.FaceLandmarks.EyebrowRightOuter.X, 2));
            distance_eyebrow_average /= 2;
            /*
            distance_eyelid_horizontal_average = Math.Sqrt(Math.Pow(face.FaceLandmarks.EyeRightOuter.Y - face.FaceLandmarks.EyeRightInner.Y, 2)
                    + Math.Pow(face.FaceLandmarks.EyeRightOuter.X - face.FaceLandmarks.EyeRightInner.X, 2));
            distance_eyelid_horizontal_average += Math.Sqrt(Math.Pow(face.FaceLandmarks.EyeLeftOuter.Y - face.FaceLandmarks.EyeLeftInner.Y, 2)
                    + Math.Pow(face.FaceLandmarks.EyeLeftOuter.X - face.FaceLandmarks.EyeLeftInner.X, 2));
            distance_eyelid_horizontal_average /= 2;
            */
            return (distance_nose_average + distance_eyebrow_average) / 2.0;
        }
        private double average_eyelid_distance(Face face)
        {
            return (Right_eyelid_distance(face) + Left_eyelid_distance(face)) / 2.0;
        }
        private double Eye_openning_ratio(Face face)
        {
            double eyelid_distance = 0, ratio = 0;
            eyelid_distance = average_eyelid_distance(face);
            ratio = eyelid_distance / Real_constant_distance(face);
            return ratio;
        }

        //below returns the ratio of how open the mouth is only using under lip (for now)

        private double yawn_ratio(Face face)
        {
            double distance_between_lips = 0, ratio = 0;
            distance_between_lips = Math.Sqrt(Math.Pow(face.FaceLandmarks.UpperLipBottom.Y - face.FaceLandmarks.UnderLipTop.Y, 2)
                    + Math.Pow(face.FaceLandmarks.UpperLipBottom.X - face.FaceLandmarks.UnderLipTop.X, 2));
            ratio = distance_between_lips / Real_constant_distance(face);
            return ratio;
        }

        // Returns a string that describes the given face

        private string FaceDescription(Face face)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Face: ");

            // Add the gender, age, and smile.
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            // Add the emotions. Display all emotions over 10%.
            sb.Append("Emotion: ");
            EmotionScores emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            // Add glasses.
            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            // Display all hair color attributes over 10%.
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
        }

        // Displays the face description when the mouse is over a face rectangle.

        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            // If the REST call has not completed, return from this method.
            if (faces == null)
                return;

            // Find the mouse position relative to the image.
            Point mouseXY = e.GetPosition(FacePhoto);

            ImageSource imageSource = FacePhoto.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;

            // Scale adjustment between the actual size and displayed size.
            var scale = FacePhoto.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);

            // Check if this mouse position is over a face rectangle.
            bool mouseOverFace = false;

            for (int i = 0; i < faces.Length; ++i)
            {
                FaceRectangle fr = faces[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                // Display the face description for this face if the mouse is over this face rectangle.
                if (mouseXY.X >= left && mouseXY.X <= left + width && mouseXY.Y >= top && mouseXY.Y <= top + height)
                {
                    faceDescriptionStatusBar.Text = faceDescriptions[i];
                    mouseOverFace = true;
                    break;
                }
            }

            // If the mouse is not over a face rectangle.
            if (!mouseOverFace)
                faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
        }
    }
}