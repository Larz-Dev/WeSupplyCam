using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.IO.Compression; // Para crear ZIPs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WeSupplyCam
{
    public partial class Editando : ContentPage
    {
        public ObservableCollection<PhotoItem> Photos { get; set; } = new ObservableCollection<PhotoItem>();

        public int cuenta = 0;
        public Editando(ObservableCollection<PhotoItem> photos,string fileTitle,int counts)
        {

            InitializeComponent();
            BindingContext = this;
            Photos = new ObservableCollection<PhotoItem>();
            Photos.Clear(); // Limpiar las fotos

            // Agregar cada foto individualmente a la colección
            foreach (var photo in photos)
            {
                Photos.Add(photo); // Agregar cada PhotoItem individualmente
            }
          
            Title = fileTitle;
          cuenta = counts;

    
            OnPropertyChanged(nameof(Photos));
          

        }


        // Constructor sin parámetros, por si no se pasan fotos
        public Editando()
        {
            InitializeComponent();
            BindingContext = this;
            Photos = new ObservableCollection<PhotoItem>();
  
               Title = string.Empty;
            cuenta = 0;
            OnPropertyChanged(nameof(Photos));
     
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
            var duplicateName = Photos
            .GroupBy(p => p.Name)   // Agrupar por nombre
            .FirstOrDefault(g => g.Count() > 1);  // Buscar el primer grupo con más de un elemento (duplicado)

            if (duplicateName != null)
            {
                // Si hay nombres duplicados, mostrar alerta y cancelar la exportación
                await DisplayAlert("Error", "Al menos un nombre de imagen está repetido. Cambie el nombre de las imágenes antes de continuar.", "OK");
                return;  // Detener el proceso
            }

            try
            {
                var exportDirectory = FileSystem.AppDataDirectory;
                string currentDate = DateTime.Now.ToString("yyyy_MM_dd");
                string currentTime = DateTime.Now.ToString("H_mm_ss");

                if (Title == "" || Title == null)
                {
                    await DisplayAlert("Error", "Falta el título.", "OK");
                    return;
                }


                string zipFilePath = Path.Combine(exportDirectory, Title+".zip");
                
                using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
                {
                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var photo in Photos)
                        {
                     var fileName = Path.GetFileName(photo.FilePath);


                       if (photo.Name == "" || photo.Name == null)
                            {
                              await  DisplayAlert("Error", "Faltan imágenes por nombrar.", "OK");
                                return;
                            }
                        zipArchive.CreateEntryFromFile(photo.FilePath, (photo.Name).ToUpper() + Path.GetExtension(photo.FilePath)); 
                        }
                    }
                }
                Title = "";
                cuenta = 0;
                OnPropertyChanged(nameof(Photos));
                Photos.Clear();



                await Share.Default.RequestAsync(new ShareFileRequest
                {
             
                    Title = "Compartir archivo ZIP",
                    File = new ShareFile(zipFilePath)
                });
          
          

                // Volver a la página principal
                Application.Current.MainPage = new NavigationPage(new MainPage());


            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear o compartir el ZIP: {ex.Message}");
                await DisplayAlert("Error", "No se pudo guardar o compartir el archivo ZIP.", "OK");
            }


        }
        public void Regresar(object sender, EventArgs e)
        {

            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
        protected override bool OnBackButtonPressed()
        {
            return true;
        }
      
         
    }
}
