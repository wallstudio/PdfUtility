using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;

namespace PdfUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            var command = args.ElementAtOrDefault(0);
            switch(command)
            {
                case nameof(Unpack):
                    Unpack(
                        srcFile: args.ElementAtOrDefault(1),
                        dstDir: args.ElementAtOrDefault(2)
                            ?? Path.Combine(Path.GetDirectoryName(args.ElementAtOrDefault(1)), Path.GetFileNameWithoutExtension(args.ElementAtOrDefault(1))));
                    break;
                case nameof(Pack):
                    Pack(
                        srcDir: args.ElementAtOrDefault(1),
                        dstFile: args.ElementAtOrDefault(2) ?? $"{args.ElementAtOrDefault(1).TrimEnd('\\', '/')}_pack.pdf");
                    break;
            }
        }

        static void Pack(string srcDir, string dstFile)
        {


            using (var document = new Document())
            using (var stream = new MemoryStream())
            using (var writer = PdfWriter.GetInstance(document, stream))
            {
                document.Open();
                var imagePaths = Directory.GetFiles(srcDir)
                    .OrderBy(name => int.Parse(Path.GetFileNameWithoutExtension(name)));
                foreach (var imagePath in imagePaths)
                {
                    var image = Image.GetInstance(imagePath);
                    image.ScaleToFit(document.PageSize);
                    document.Add(image);
                    document.NewPage();
                }
                document.Close();
                File.WriteAllBytes(dstFile, stream.ToArray());
            }
        }

        class ReaderListener : IRenderListener
        {
            public Action<ImageRenderInfo> OnRenderImage { private get; init; }
            void IRenderListener.BeginTextBlock() {}
            void IRenderListener.EndTextBlock() {}
            void IRenderListener.RenderImage(ImageRenderInfo renderInfo) => OnRenderImage?.Invoke(renderInfo);
            void IRenderListener.RenderText(TextRenderInfo renderInfo) {}
        }

        static void Unpack(string srcFile, string dstDir)
        {
            using (var reader = new PdfReader(srcFile))
            {
                var parser = new PdfReaderContentParser(reader);
                var imageinfos = new List<ImageRenderInfo>();
                var listener = new ReaderListener(){ OnRenderImage = img => imageinfos.Add(img)};
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    parser.ProcessContent(i, listener);
                }

                var images = imageinfos
                    .Select(info => info.GetImage())
                    .Select((img, i) => (img.GetImageAsBytes(), $"{i}.{img.GetFileType()}"));
                
                Directory.CreateDirectory(dstDir);
                foreach(var (bytes, fileName) in images)
                {
                    File.WriteAllBytes(Path.Combine(dstDir, fileName), bytes);
                }

            }
        }
    }
}
