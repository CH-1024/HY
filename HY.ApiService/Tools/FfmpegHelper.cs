using System.Diagnostics;
using System.Text;

namespace HY.ApiService.Tools
{
    public class FFmpegHelper
    {
        private readonly string FFmpegCommand;

        public FFmpegHelper(string path)
        {
            FFmpegCommand = path;
        }

        // 检查 ffmpeg 是否在系统 PATH 中可用
        public bool IsFFmpegAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = FFmpegCommand,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(3000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        // 提取帧（原方法）
        public async Task<bool> ExtractFrameAsync(string inputVideo, string outputImage, double timeSeconds)
        {
            string arguments = $"-ss {timeSeconds:F3} -i \"{inputVideo}\" -vframes 1 -q:v 2 \"{outputImage}\" -y";
            return await RunFFmpegAsync(arguments, outputImage);
        }

        // 新增：CRF压缩
        public async Task<bool> CompressVideoAsync(string inputVideo, string outputVideo, int crf = 23, string preset = "medium", string? audioBitrate = "128k", int scaleWidth = 0, int scaleHeight = 0)
        {
            string videoFilter = BuildScaleFilter(scaleWidth, scaleHeight);
            string audioParams = audioBitrate != null ? $"-c:a aac -b:a {audioBitrate}" : "-c:a copy";

            string arguments = $"-i \"{inputVideo}\" " +
                               $"-c:v libx264 -crf {crf} -preset {preset} " +
                               $"{videoFilter} " +
                               $"{audioParams} " +
                               $"-movflags +faststart " +
                               $"-y \"{outputVideo}\"";
            return await RunFFmpegAsync(arguments, outputVideo);
        }

        // 新增：固定码率压缩
        public async Task<bool> CompressVideoWithBitrateAsync(string inputVideo, string outputVideo, string videoBitrate = "1M", string audioBitrate = "128k", int scaleWidth = 0, int scaleHeight = 0)
        {
            string videoFilter = BuildScaleFilter(scaleWidth, scaleHeight);

            string arguments = $"-i \"{inputVideo}\" " +
                               $"-c:v libx264 -b:v {videoBitrate} -maxrate {videoBitrate} -bufsize {videoBitrate} " +
                               $"{videoFilter} " +
                               $"-c:a aac -b:a {audioBitrate} " +
                               $"-movflags +faststart " +
                               $"-y \"{outputVideo}\"";
            return await RunFFmpegAsync(arguments, outputVideo);
        }










        // 私有辅助：构建缩放滤镜
        private string BuildScaleFilter(int width, int height)
        {
            if (width > 0 && height > 0)
                return $"-vf scale={width}:{height}";
            if (width > 0)
                return $"-vf scale={width}:-1";
            if (height > 0)
                return $"-vf scale=-1:{height}";
            return "";
        }

        // 私有执行方法
        private async Task<bool> RunFFmpegAsync(string arguments, string expectedOutputFile)
        {
            var psi = new ProcessStartInfo
            {
                FileName = FFmpegCommand,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var errorBuilder = new StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(expectedOutputFile))
                {
                    Console.WriteLine($"操作成功：{expectedOutputFile}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"ffmpeg 执行失败，退出代码 {process.ExitCode}");
                    Console.WriteLine("错误输出：");
                    Console.WriteLine(errorBuilder.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调用 ffmpeg 时发生异常：{ex.Message}");
                return false;
            }
        }
    }
}
