using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Xabe.FFmpeg;

namespace make_it_all_in_one.Utils
{

    public class FFmpegHelper
    {

        public FFmpegHelper()
        {
            // init ffmpeg
            FFmpeg.SetExecutablesPath(Path.Combine(AppContext.BaseDirectory, "Assets"));
        }
        public class MediaInfo
        {
            public List<StreamDetail>? streams { get; set; }
        }
        public class StreamDetail
        {
            public int? index { get; set; }
            public string? codec_name { get; set; }
            public string? codec_long_name { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public Disposition? disposition { get; set; }
        }

        public class Disposition
        {
            public int? attached_pic { get; set; }
        }

        public async Task<List<StreamDetail>> GetStreamDetails(string filePath)
        {
            var result = new List<StreamDetail>();
            var mediaInfo = await new Probe().Start($"-v quiet -print_format json -show_streams \"{filePath}\"");
            Debug.WriteLine($"=== MediaInfo of {Path.GetFileName(filePath)} ===");
            Debug.WriteLine(mediaInfo);
            Debug.WriteLine($"===================");
            var mediaInfoObject = JsonSerializer.Deserialize<MediaInfo>(mediaInfo.ToString());
            if (mediaInfoObject?.streams == null)
            {
                throw new Exception("Failed to parse stream details.");
            }
            var streamDetails = mediaInfoObject.streams;

            foreach (var streamDetail in streamDetails)
            {
                var detail = new StreamDetail
                {
                    index = streamDetail.index,
                    codec_name = streamDetail.codec_name,
                    codec_long_name = streamDetail.codec_long_name,
                    width = streamDetail.width,
                    height = streamDetail.height,
                    disposition = new Disposition
                    {
                        attached_pic = streamDetail.disposition?.attached_pic
                    }
                };
                result.Add(detail);
            }

            return result;
        }

        public List<int> GetPosterIndexFromVideo(List<StreamDetail> input)
        {
            var result = new List<int>();

            foreach (var item in input)
            {
                if (item.disposition != null && item.index != null && item.disposition.attached_pic == 1)
                {
                    result.Add(item.index.Value);
                }
            }

            return result;
        }

        public async Task<string> GetPosterFromVideo(string videoPath, List<StreamDetail> streamDetails)
        {
            // Get the index of the first stream with attached picture disposition
            var posterIndexes = GetPosterIndexFromVideo(streamDetails);
            if (posterIndexes.Count != 1)
            {
                throw new Exception("No poster stream found or multiple poster streams found.");
            }
            var posterIndex = posterIndexes[0];

            // Get the codec name of the poster stream
            string? tempImgExt = streamDetails[posterIndex].codec_name == "mjpeg" ? ".jpg" : null;
            if (tempImgExt == null)
            {
                throw new Exception("Unsupported codec for poster stream.");
            }

            // Create a temporary file for the poster image
            string tempImg = Path.GetTempFileName() + tempImgExt;

            // Execute the FFmpeg command to extract the poster image
            var cmd =
                $" -loglevel warning" + // Show only warnings and errors in log output
                $" -y" + // enforce overwrite
                $" -i \"{videoPath}\"" + // Input video
                $" -map 0:{posterIndex}" + // Select the poster stream
                $" -c copy" + // Use stream copy mode (no re-encoding)
                $" \"{tempImg}\""; // Output file path
            Debug.WriteLine($"Executing FFmpeg Command: {cmd}");
            await FFmpeg.Conversions
                .New()
                .Start(cmd);

            Debug.WriteLine($"Poster image extracted to: {tempImg}");
            return tempImg;
        }

        // Saves the video file with the poster image attached
        public async Task SaveVideoFile(string videoPath, string? posterPath, StorageFile outputFile, List<StreamDetail> streamDetails)
        {
            // Get the index of the first stream with attached picture disposition
            var posterIndexes = GetPosterIndexFromVideo(streamDetails);
            if (posterIndexes.Count > 1)
            {
                throw new Exception("Multiple existing posters found.");
            }
            int? posterIndex = posterIndexes.Count == 0 ? null : posterIndexes[0];

            // Execute the FFmpeg command
            List<string> cmd = new List<string>();
            cmd.Add("-loglevel warning"); // Show only warnings and errors in log output
            cmd.Add("-y");  // Overwrite existing file
            cmd.Add($"-i \"{videoPath}\""); // Input video
            if (posterPath != null) cmd.Add($"-i \"{posterPath}\""); // Input image
            cmd.Add("-map 0"); // Include all streams from the first input (original video)
            if (posterIndex != null) cmd.Add($"-map -0:{posterIndex}"); // Exclude the existing poster stream
            if (posterPath != null) cmd.Add("-map 1"); // Include all streams from the second input (the image)
            cmd.Add("-c copy"); // Use stream copy mode (no re-encoding)
            if (posterPath != null) cmd.Add("-disposition:v:1 attached_pic"); // Set disposition for the second video stream (the image) to "attached_pic"
            cmd.Add($"\"{outputFile.Path}\""); // Output file path
            Debug.WriteLine($"Executing FFmpeg Command: {string.Join(" ", cmd)}");
            var conversion = FFmpeg.Conversions.New();
            conversion.OnProgress += (sender, args) =>
            {
                Debug.WriteLine($"[{args.Duration}/{args.TotalLength}][{args.Percent}%]");
            };
            try
            {
                await conversion.Start(string.Join(" ", cmd));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Error] during FFmpeg conversion: {ex.Message}");
                throw;
            }

            Debug.WriteLine("FFmpeg Command Completed");
            // Defer and complete file updates for proper file system integration
            CachedFileManager.DeferUpdates(outputFile);
            await CachedFileManager.CompleteUpdatesAsync(outputFile);
        }

    }
}
