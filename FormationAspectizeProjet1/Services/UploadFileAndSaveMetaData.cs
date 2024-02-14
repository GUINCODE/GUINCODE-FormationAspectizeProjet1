using System;
using System.Security;
using Microsoft.SharePoint.Client;
using Aspectize.Core;
using System.IO;
using System.Net;
using System.Linq;
using Upload;

namespace FormationAspectizeProjet1.Services
{
    public interface IUploadFileAndSaveMetaData
    {
        bool UploadAndSave(UploadedFile[] uploadedFiles, string titreDocument, string description, string autreInfos);
    }

    [Service(Name = "UploadFileAndSaveMetaData")]
    public class UploadFileAndSaveMetaData : IUploadFileAndSaveMetaData //, IInitializable, ISingleton
    {
        public bool UploadAndSave(UploadedFile[] uploadedFiles, string titreDocument, string description, string autreInfos)
        {

            var result = false;
            result =IisCorrectDataReceived(uploadedFiles, titreDocument, description,autreInfos);

            if(result == false)
            {
                System.Diagnostics.Debug.WriteLine("Les donn�es ne sont pas bonnes ");
                return false;
            }

            SaveMetaData(titreDocument, description, autreInfos);

            // Forcer l'utilisation de TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string userName = "agb@gncoder.onmicrosoft.com";
            string Password = "@P@$$w@rd2030";
            var securePassword = new SecureString();
            foreach (char c in Password)
            {
                securePassword.AppendChar(c);
            }

            try
            {
                using (var ctx = new ClientContext("https://gncoder.sharepoint.com/sites/guincode"))
                {
                    // Activer la journalisation d�taill�e
                    ctx.ExecutingWebRequest += (sender, e) =>
                    {
                        System.Diagnostics.Debug.WriteLine(e.WebRequestExecutor);
                    };

                    ctx.Credentials = new Microsoft.SharePoint.Client.SharePointOnlineCredentials(userName, securePassword);
                    Web web = ctx.Web;
                    ctx.Load(web);
                    ctx.ExecuteQuery();

                    List byTitle = ctx.Web.Lists.GetByTitle("MaBibliotheque");

                    // Logique pour t�l�verser les fichiers dans la biblioth�que...
                    foreach (UploadedFile uploadedFile in uploadedFiles)
                    {
                        try
                        {
                            string fileName = uploadedFile.Name; // Nom du fichier � uploader
                            Stream fileStream = uploadedFile.Stream; // Stream du fichier

                            // Assurez-vous d'�tre connect� au contexte de SharePoint et d'avoir s�lectionn� la biblioth�que
                            List library = ctx.Web.Lists.GetByTitle("MaBibliotheque");

                            var fileCreationInformation = new FileCreationInformation
                            {
                                ContentStream = fileStream, // Mettez le contenu du fichier ici
                                Url = fileName, // Le nom du fichier dans SharePoint
                                Overwrite = true // �crase le fichier s'il existe d�j�
                            };

                            // Upload le fichier dans la biblioth�que
                            Microsoft.SharePoint.Client.File uploadFile = library.RootFolder.Files.Add(fileCreationInformation);
                            ctx.Load(uploadFile);
                            ctx.ExecuteQuery();

                            System.Diagnostics.Debug.WriteLine($"Fichier uploder sur sharepoint : {fileCreationInformation.Url}");

                            //ici on appelle la fonction permettant de stocker les m�ta-donn�es du fichier

                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur lors de l'upload du fichier dans SharePoint: {ex.Message}");
                            // G�rez l'erreur comme n�cessaire
                            return false;
                        }
                        
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Une erreur est survenue: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"D�tails de l'erreur interne: {ex.InnerException.Message}");
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception de type Exception :�{ex.Message}");
                return false;
            }

            return false;
        }


         bool IisCorrectDataReceived(UploadedFile[] uploadedFiles, string titreDocument, string description, string autreInfos)
        {

            //System.Diagnostics.Debug.WriteLine($"taille tableau de fichier: {uploadedFiles.Length}");
            // V�rifier si l'objet attributes est null
            if (uploadedFiles == null)
            {
                System.Diagnostics.Debug.WriteLine($"tableau de fichier null");

                return false;
            }
            if (uploadedFiles.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"tableau de fichier vide");

                return false;
            }

            if (uploadedFiles[0].Stream != null)
            {
                System.Diagnostics.Debug.WriteLine("le strem n'est pas null");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("le strem est pas null");
                return false;

            }
            foreach (var file in uploadedFiles)
            {
                System.Diagnostics.Debug.WriteLine($"Nom de fichier: {file.Name}, Taille: {file.ContentLength} bytes");
            }


          

            // V�rifier si les champs sp�cifiques sont nuls ou vides
            bool isTitreDocumentValid = !string.IsNullOrEmpty(titreDocument);
            bool isDescriptionDocumentValid = !string.IsNullOrEmpty(description);
            bool isAutreInfosValid = !string.IsNullOrEmpty(autreInfos);

            // Si tous les champs requis sont non nuls et non vides, retourner true

            var result = isTitreDocumentValid && isDescriptionDocumentValid && isAutreInfosValid;
            System.Diagnostics.Debug.WriteLine($"resultat est: {result}");
            return result;
        }

         bool SaveMetaData( string titreDocument, string description, string autreInfos)
        {
            IDataManager dm = EntityManager.FromDataBaseService("DataService");
            IEntityManager em = dm as IEntityManager;

            var metaData = em.CreateInstance<DocumentInfo>();
            metaData.Name = titreDocument;
           // metaData.DateAjout = DateTime.Now; // Pour    // ou
            metaData.DateAjout = DateTime.UtcNow; // Pour l'heure UTC
            metaData.AutreInfos = autreInfos;
            metaData.Description = description;
            metaData.Taille = "100 ko";
            metaData.Type = "PDF";

            dm.SaveTransactional();
            System.Diagnostics.Debug.WriteLine("Fonction enregistrement de meta data effectu�");
            return true;
        }

    }

}