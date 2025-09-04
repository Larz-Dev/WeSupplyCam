using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;
using System.Diagnostics;

namespace WeSupplyCam
{
    public partial class Archivos : ContentPage
    {
        public ObservableCollection<FileItem> Elementos { get; set; }
        public ObservableCollection<PhotoItem> Photos { get; set; } = new ObservableCollection<PhotoItem>();

        public Archivos()
        {
            InitializeComponent();

            Elementos = new ObservableCollection<FileItem>();
            Photos = new ObservableCollection<PhotoItem>();
            BindingContext = this;

            
        }

        protected override  void OnAppearing()
        {
            base.OnAppearing();
            SelectFiles();
        }









        public async void Eliminar(object sender, EventArgs e)
        {
            var fileitem = (FileItem)((Button)sender).BindingContext;
            if (await DisplayAlert("Confirmar", $"¿Eliminar {fileitem.Name}?", "Sí", "No"))
            {
                Elementos.Remove(fileitem);


                if (File.Exists(fileitem.FilePath))
                {
                    File.Delete(fileitem.FilePath);
                }
            }
        }

        public async void Compartir(object sender, EventArgs e)
        {
            var fileitem = (FileItem)((Button)sender).BindingContext;

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartir archivo ZIP",
                File = new ShareFile(fileitem.FilePath)
            });
        }

        private async void SelectFiles()
        {
            var arhivosZip = 0;
            var files = Directory.GetFiles(FileSystem.AppDataDirectory);

            Elementos.Clear();

            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains(".zip"))
                {
                    arhivosZip += 1;
                    Elementos.Add(new FileItem { FilePath = files[i], Name = Path.GetFileName(files[i]) });
                }
            }

            if (arhivosZip == 0)
            {
         
            }
        }

        public async void Editar(object sender, EventArgs e )
        {
            var fileitem = (FileItem)((Button)sender).BindingContext;

            try
            {
                // Ruta temporal donde se extraerán las imágenes
                DateTime utcDate = DateTime.UtcNow;
                string tempDirectory = Path.Combine(FileSystem.AppDataDirectory, "TempImages" + utcDate.ToLongTimeString());
                Directory.CreateDirectory(tempDirectory); // Crear el directorio temporal si no existe

                // Limpiar la lista de fotos actuales antes de agregar nuevas
                Photos.Clear();

                // Abrir el archivo ZIP para lectura
                using (var zipArchive = ZipFile.Open(fileitem.FilePath, ZipArchiveMode.Update)) // Abrir con modo de actualización
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        // Solo procesar imágenes con extensiones .jpg, .png, .jpeg, .webp
                        if (entry.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                        {
                            // Definir la ruta completa de extracción
                            string extractedFilePath = Path.Combine(tempDirectory, entry.FullName);

                            // Asegurar que el directorio donde se extraerá el archivo exista
                            Directory.CreateDirectory(Path.GetDirectoryName(extractedFilePath));

                            // Extraer el archivo y sobrescribir si ya existe
                            entry.ExtractToFile(extractedFilePath, overwrite: true);

                            // Verificar si el archivo fue extraído correctamente
                            if (File.Exists(extractedFilePath))
                            {
                                Console.WriteLine($"Imagen extraída correctamente: {extractedFilePath}");
                                // Obtener solo el nombre del archivo sin la extensión
                                Photos.Add(new PhotoItem { FilePath = extractedFilePath, Name = Path.GetFileNameWithoutExtension(entry.Name) });
                            }
                            else
                            {
                                Console.WriteLine($"Error al extraer la imagen: {extractedFilePath}");
                            }
                        }
                    }
                }

                // Verificar si la colección de fotos tiene elementos
                if (Photos.Count > 0)
                {
                    // Ahora, actualizamos el archivo ZIP con las imágenes extraídas
                    using (var zipArchive = ZipFile.Open(fileitem.FilePath, ZipArchiveMode.Update))
                    {
                        foreach (var photo in Photos)
                        {
                            // Verificar si ya existe una entrada en el ZIP con el mismo nombre
                            var entryName = photo.Name.ToUpper() + Path.GetExtension(photo.FilePath);
                            var existingEntry = zipArchive.GetEntry(entryName);

                            if (existingEntry != null)
                            {
                                // Si existe, eliminar la entrada antigua para sobrescribir
                                existingEntry.Delete();
                                Console.WriteLine($"Imagen {photo.Name} eliminada para sobrescribir.");
                            }

                            // Crear una nueva entrada con la nueva imagen (usando el nombre de la foto)
                            zipArchive.CreateEntryFromFile(photo.FilePath, entryName);
                            Console.WriteLine($"Imagen {photo.Name} añadida al ZIP.");
                        }
                    }

                    // Ahora empujamos las fotos al MainPage
                    // Assuming this is inside an async method
                   
                    await Navigation.PushAsync(new MainPage(Photos, Path.GetFileNameWithoutExtension(fileitem.FilePath), Photos.Count));
                }
                else
                {
                    await DisplayAlert("Error", "No se han encontrado imágenes en el archivo ZIP.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Capturar cualquier excepción y mostrar un mensaje de error
                Console.WriteLine($"Error al extraer las imágenes: {ex.Message}");
                await DisplayAlert("Error", "No se pudo extraer las imágenes del archivo ZIP.", "OK");
            }
        }
    
        public class FileItem
        {
            public string FilePath { get; set; }
            public string Name { get; set; }
        }
    }
}
