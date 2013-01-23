using System;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;

namespace ClientAlarm
{
    public partial class ClientShowVideoForm : Form
    {
        private const int WIDTH_CAM = 800;
        private const int HEIGHT_CAM = 600;
        private VideoCaptureDevice videoCamera;
        private Boolean isRunCamera;

        public ClientShowVideoForm(VideoCaptureDevice videoCamera)
        {
            InitializeComponent();

            this.FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if(this.videoCamera != null && !isRunCamera)
                    this.videoCamera.Stop();
            };

            this.videoCamera = videoCamera;
            this.isRunCamera = this.videoCamera.IsRunning;
            videoPlayer.VideoSource = this.videoCamera;
            this.videoCamera.DesiredFrameSize = new System.Drawing.Size(WIDTH_CAM, HEIGHT_CAM);
            this.videoCamera.DesiredSnapshotSize = new System.Drawing.Size(WIDTH_CAM, HEIGHT_CAM);

            this.Width += (WIDTH_CAM - this.Width);
            this.Height += (HEIGHT_CAM - this.Height);

            if(!isRunCamera)
                this.videoCamera.Start();
        }
    }
}
