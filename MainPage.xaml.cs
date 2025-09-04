using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.IO.Compression; // Para crear ZIPs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

namespace WeSupplyCam
{
    public partial class MainPage : ContentPage
    {

        public ObservableCollection<PhotoItem> Photos { get; set; } = new ObservableCollection<PhotoItem>();

        public int cuenta = 0;
        public string TitleFile { get; set; }



        public MainPage(ObservableCollection<PhotoItem> photos, string fileTitle, int counts)
        {


            InitializeComponent();
            TitleFile = fileTitle;

            Photos = new ObservableCollection<PhotoItem>();
            Photos.Clear(); // Limpiar las fotos

            // Agregar cada foto individualmente a la colección
            foreach (var photo in photos)
            {
                Photos.Add(photo); // Agregar cada PhotoItem individualmente
            }
            Title = "WeSupplyCam";
            cuenta = counts;


            OnPropertyChanged(nameof(Photos));
            BindingContext = this;
        }


        // Constructor sin parámetros, por si no se pasan fotos
        public MainPage()
        {


            Title = "WeSupplyCam";
            // Accede al valor de TitleFile cuando sea necesario

            InitializeComponent();

            Photos = new ObservableCollection<PhotoItem>();


            cuenta = 0;
            OnPropertyChanged(nameof(Photos));

            BindingContext = this;
        }




        // Tomar una foto desde la galería sin guardarla
        private async void SelectPhotoAsync(object sender, EventArgs e)
        {
            try
            {

                var photo = await FilePicker.PickMultipleAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images // Limita la selección a solo imágenes
                });

                if (photo != null && photo.Count() > 0)
                {


                    foreach (var item in photo)
                    {
                        if (item != null)
                        {
                            OnPropertyChanged(nameof(Photos));
                            cuenta += 1;


                            Photos.Add(new PhotoItem { FilePath = item.FullPath, Name = cuenta.ToString() });

                        }
                    }
                }
            }



            catch (Exception ex)
            {
                Console.WriteLine($"Error al seleccionar la foto: {ex.Message}");
                await DisplayAlert("Error", "No se pudo seleccionar la foto.", "OK");
            }
        }


        private async void TakePhotoAsync(object sender, EventArgs e)
        {
            try
            {


                var photo = await MediaPicker.CapturePhotoAsync();


                if (photo != null)
                {

                    OnPropertyChanged(nameof(Photos));
                    cuenta += 1;
                    Photos.Add(new PhotoItem { FilePath = photo.FullPath, Name = cuenta.ToString() });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al tomar la foto: {ex.Message}");
                await DisplayAlert("Error", "No se pudo tomar la foto.", "OK");
            }
        }


        private void RemovePhotoAsync(object sender, EventArgs e)
        {

            var photoItem = (PhotoItem)((Button)sender).BindingContext;
            Photos.Remove(photoItem);
            BindingContext = this;

        }

        private async void SaveAndShareZipAsync(object sender, EventArgs e)
        {
            activityIndicator.IsRunning = true;
            activityIndicator.IsVisible = true;

            var duplicateName = Photos
            .GroupBy(p => p.Name)   // Agrupar por nombre
            .FirstOrDefault(g => g.Count() > 1);  // Buscar el primer grupo con más de un elemento (duplicado)

            if (duplicateName != null)
            {
                // Si hay nombres duplicados, mostrar alerta y cancelar la exportación
                await DisplayAlert("Error", "Al menos un nombre de imagen está repetido. Cambie el nombre de las imágenes antes de continuar.", "OK");
                activityIndicator.IsRunning = false;
                activityIndicator.IsVisible = false;
                return;  // Detener el proceso
            }

            try
            {
                var exportDirectory = FileSystem.AppDataDirectory;
                string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                string currentTime = DateTime.Now.ToString("H_mm_ss");

                if (TitleFile == "" || TitleFile == null)
                {
                    await DisplayAlert("Error", "Falta el título.", "OK");
                    activityIndicator.IsRunning = false;
                    activityIndicator.IsVisible = false;
                    return;
                }


                string zipFilePath = Path.Combine(exportDirectory, TitleFile + ".zip");

                using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
                {
                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var photo in Photos)
                        {
                            var fileName = Path.GetFileName(photo.FilePath);


                            if (photo.Name == "" || photo.Name == null)
                            {
                                await DisplayAlert("Error", "Faltan imágenes por nombrar.", "OK");
                                activityIndicator.IsRunning = false;
                                activityIndicator.IsVisible = false;
                                return;

                            }

                            zipArchive.CreateEntryFromFile(photo.FilePath, (photo.Name).ToUpper() + Path.GetExtension(photo.FilePath), CompressionLevel.SmallestSize);
                        }
                    }
                }

                TitleFile = "";
                cuenta = 0;
                OnPropertyChanged(nameof(Photos));
                OnPropertyChanged(nameof(TitleFile));

                Photos.Clear();


                await Share.Default.RequestAsync(new ShareFileRequest
                {

                    Title = "Compartir archivo ZIP",
                    File = new ShareFile(zipFilePath)
                });
                activityIndicator.IsRunning = false;
                activityIndicator.IsVisible = false;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear o compartir el ZIP: {ex.Message}");
                await DisplayAlert("Error", "No se pudo guardar o compartir el archivo ZIP.", "OK");
            }
        }


        public async void ExportPhoto(object sender, EventArgs e)
        {



            var photoItem = (PhotoItem)((Button)sender).BindingContext;

            BindingContext = this;
            await Share.Default.RequestAsync(new ShareFileRequest
            {

                Title = "Compartir imagen" + photoItem.Name,
                File = new ShareFile(photoItem.FilePath)
            });
           



        

        }
        async private void ExportPDF(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleFile))
            {
                await DisplayAlert("Error", "Falta el título.", "OK");
                activityIndicator.IsRunning = false;
                activityIndicator.IsVisible = false;
                return;
            }



            activityIndicator.IsRunning = true;
            activityIndicator.IsVisible = true;

            var duplicateName = Photos
            .GroupBy(p => p.Name)   // Agrupar por nombre
            .FirstOrDefault(g => g.Count() > 1);  // Buscar el primer grupo con más de un elemento (duplicado)

            if (duplicateName != null)
            {
                // Si hay nombres duplicados, mostrar alerta y cancelar la exportación
                await DisplayAlert("Error", "Al menos un nombre de imagen está repetido. Cambie el nombre de las imágenes antes de continuar.", "OK");
                activityIndicator.IsRunning = false;
                activityIndicator.IsVisible = false;
                return;  // Detener el proceso
            }

            try
            {
                var exportDirectory = FileSystem.AppDataDirectory;
                string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                string currentTime = DateTime.Now.ToString("H_mm_ss");

                if (TitleFile == "" || TitleFile == null)
                {
                    await DisplayAlert("Error", "Falta el título.", "OK");
                    activityIndicator.IsRunning = false;
                    activityIndicator.IsVisible = false;
                    return;
                }


                string zipFilePath = Path.Combine(exportDirectory, TitleFile + ".zip");

                using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
                {
                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var photo in Photos)
                        {
                            var fileName = Path.GetFileName(photo.FilePath);


                            if (photo.Name == "" || photo.Name == null)
                            {
                                await DisplayAlert("Error", "Faltan imágenes por nombrar.", "OK");
                                activityIndicator.IsRunning = false;
                                activityIndicator.IsVisible = false;
                                return;

                            }

                            zipArchive.CreateEntryFromFile(photo.FilePath, (photo.Name).ToUpper() + Path.GetExtension(photo.FilePath), CompressionLevel.SmallestSize);
                        }
                    }
                }

              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear o compartir el ZIP: {ex.Message}");
                await DisplayAlert("Error", "No se pudo guardar o compartir el archivo ZIP.", "OK");
            }





            PdfDocument document = new PdfDocument();
            XFont font = new XFont("Arial", 10); // Fuente más pequeña para mejor ajuste

            // Configuración de la distribución (2 columnas x 3 filas por página)
            int columns = 2, rows = 3;
            int imagesPerPage = columns * rows;
            int imgIndex = 0;

            // Tamaño de imagen ajustado para evitar que se desborde
            int imageWidth = 220, imageHeight = 220;
            int textHeight = 20;
            int spacingX = 50, spacingY = 2; // Espaciado mínimo entre imágenes

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XTextFormatter tf = new XTextFormatter(gfx);

            foreach (var item in Photos)
            {
                // Si ya llenamos la página, creamos una nueva
                if (imgIndex > 0 && imgIndex % imagesPerPage == 0)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    tf = new XTextFormatter(gfx);
                }

                // Cálculo de la posición en la cuadrícula
                int row = (imgIndex % imagesPerPage) / columns; // Fila actual (0-2)
                int col = (imgIndex % imagesPerPage) % columns; // Columna actual (0-1)
                int marginX = 50, marginY = 50;  // Margen desde el borde de la página

                // Coordenadas para la imagen y el texto (sin margen extra)
                int x = marginX + col * (imageWidth + spacingX);
                int y = marginY + row * (imageHeight + textHeight + spacingY);

                // Dibujar la imagen ajustada
                DrawImage(gfx, item.FilePath, x, y, imageWidth, imageHeight);

                // Dibujar el texto debajo de la imagen (centrado)
                double textWidth = gfx.MeasureString(item.Name, font).Width;
                double centerX = x + (imageWidth - textWidth) / 2;
                tf.DrawString(item.Name, font, XBrushes.Black, new XRect(centerX, y + imageHeight + 5, textWidth, textHeight), XStringFormats.TopLeft);

                imgIndex++;
            }

            // Guardar el PDF
            string pdfPath = Path.Combine(FileSystem.AppDataDirectory, TitleFile + ".pdf");
            document.Save(pdfPath);



            // Compartir el PDF
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir pdf " + TitleFile,
                File = new ShareFile(pdfPath)
            });
            TitleFile = "";
            cuenta = 0;
            OnPropertyChanged(nameof(Photos));
            OnPropertyChanged(nameof(TitleFile));

            Photos.Clear();
            activityIndicator.IsRunning = false;
            activityIndicator.IsVisible = false;
        }

        void DrawImage(XGraphics gfx, string imagePath, int x, int y, int width, int height)
        {
            XImage image = XImage.FromFile(imagePath);
            gfx.DrawImage(image, x, y, width, height);
        }
    }
    }
