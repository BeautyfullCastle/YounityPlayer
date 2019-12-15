using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using YoutubeExplode;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubePlayer
{
    /// <summary>
    /// Downloads and plays Youtube videos a VideoPlayer component
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class YoutubePlayer : MonoBehaviour
    {
        /// <summary>
        /// VideoStartingDelegate 
        /// </summary>
        /// <param name="url">Youtube url (e.g. https://www.youtube.com/watch?v=VIDEO_ID)</param>
        public delegate void VideoStartingDelegate(string url);

        /// <summary>
        /// Event fired when a youtube video is starting
        /// Useful to start downloading captions, etc.
        /// </summary>
        public event VideoStartingDelegate YoutubeVideoStarting;

        private VideoPlayer videoPlayer;
        private YoutubeClient youtubeClient;

        private string id = null;

        private void Awake()
        {
            youtubeClient = new YoutubeClient();
            videoPlayer = GetComponent<VideoPlayer>();
        }

        public void PlayVideoByIdAsync(string id = null, Action<string> onComplete = null)
        {
            try
            {
                id = id ?? this.id;
                this.id = id;
                WaitForAsync(id, (streamInfoSet) => 
                {
                    var streamInfo = streamInfoSet.WithHighestVideoQualitySupported();
                    if (streamInfo == null)
                        throw new NotSupportedException($"No supported streams in youtube video '{id}'");

                    videoPlayer.source = VideoSource.Url;

                    //Resetting the same url restarts the video...
                    if (videoPlayer.url != streamInfo.Url)
                        videoPlayer.url = streamInfo.Url;

                    videoPlayer.Play();

                    onComplete?.Invoke(id);
                    YoutubeVideoStarting?.Invoke(id);
                });
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void WaitForAsync(string id, Action<MediaStreamInfoSet> onComplete = null)
        {
            StartCoroutine(WaitForAsyncCoroutine(id, onComplete));
        }

        private IEnumerator WaitForAsyncCoroutine(string id, Action<MediaStreamInfoSet> onComplete)
        {
            var streamInfoSetTask = youtubeClient.GetVideoMediaStreamInfosAsync(id);

            while (!streamInfoSetTask.IsCompleted)
            {
                yield return null;
            }

            if (streamInfoSetTask.IsFaulted)
            {
                throw streamInfoSetTask.Exception;
            }

            onComplete?.Invoke(streamInfoSetTask.Result);
        }
        
        /// <summary>
        /// Download a youtube video to a destination folder
        /// </summary>
        /// <param name="destinationFolder">A folder to create the file in</param>
        /// <param name="id">Youtube url (e.g. https://www.youtube.com/watch?v=VIDEO_ID)</param>
        /// <param name="progress">An object implementing `IProgress` to get download progress, from 0 to 1</param>
        /// <param name="cancellationToken">A CancellationToken used to cancel the current async task</param>
        /// <returns>Returns the path to the file where the video was downloaded</returns>
        /// <exception cref="NotSupportedException">When the youtube url doesn't contain any supported streams</exception>
        public async Task<string> DownloadVideoAsync(string destinationFolder = null, string id = null,
            IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                id = id ?? this.id;
                var video = await youtubeClient.GetVideoAsync(id);
                var streamInfoSet = await youtubeClient.GetVideoMediaStreamInfosAsync(id);
                
                cancellationToken.ThrowIfCancellationRequested();
                var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
                if (streamInfo == null)
                    throw new NotSupportedException($"No supported streams in youtube video '{id}'");

                var fileExtension = streamInfo.Container.GetFileExtension();
                var fileName = $"{video.Title}.{fileExtension}";

                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var invalidChar in invalidChars)
                {
                    fileName = fileName.Replace(invalidChar.ToString(), "_");
                }

                var filePath = fileName;
                if (!string.IsNullOrEmpty(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                    filePath = Path.Combine(destinationFolder, fileName);
                }

                using (var output = File.Create(filePath))
                    await youtubeClient.DownloadMediaStreamAsync(streamInfo, output, progress, cancellationToken);

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Download closed captions for a youtube video
        /// </summary>
        /// <param name="id">Youtube url (e.g. https://www.youtube.com/watch?v=VIDEO_ID)</param>
        /// <returns>A ClosedCaptionTrack object.</returns>
        public async Task<ClosedCaptionTrack> DownloadClosedCaptions(string id = null)
        {
            id = id ?? this.id;
            var trackInfos = await youtubeClient.GetVideoClosedCaptionTrackInfosAsync(id);
            if (trackInfos?.Count == 0)
                return null;

            var trackInfo = trackInfos.FirstOrDefault(t => t.Language.Code == "en") ?? trackInfos.First();
            return await youtubeClient.GetClosedCaptionTrackAsync(trackInfo);
        }

        /// <summary>
        /// Try to parse a video ID from a video Url.
        /// If null is passed, it will use the current url of the Youtube Player instance.
        /// </summary>
        /// <param name="videoUrl">Youtube url (e.g. https://www.youtube.com/watch?v=VIDEO_ID)</param>
        /// <returns>The video ID</returns>
        /// <exception cref="ArgumentException">If the videoUrl is not a valid youtube url</exception>
        public string GetVideoId(string videoUrl = null)
        {
            if (!YoutubeClient.TryParseVideoId(videoUrl, out var videoId))
                throw new ArgumentException(string.Format("Invalid youtube url: {0}", videoUrl), nameof(videoUrl));

            return videoId;
        }
    }
}