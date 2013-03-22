using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
/*
 * Json.NET  (Se usa para parsear la respuesta de las APIS de google)
 * 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
*/

// -----------------------------------------------------------------------------------
// -----------------------------------------------------------------------------------
// NOTA:
// Parece ser que google bloquea la API a partir de 100 peticiones AL DÍA,
// por lo que el código de buscar el IMDB link y la imagen sirven para más bien poco.
// -----------------------------------------------------------------------------------
// -----------------------------------------------------------------------------------

namespace ParseTxTconElinks
{
    public partial class Form1 : Form
    {
        class Links_misma_Serie
        {
            public string NombreSerie;
            public StringCollection eLinks_RAW = new StringCollection();
            public StringCollection eLinks_decoded = new StringCollection();
            public string Image_Link;
            public string IMDB_Link;
        };

        class TXT_Procesado
        {
            public string Path_TXT;
            public List<Links_misma_Serie> Lista_Series;
        }

        string HTML_Final { get; set; }
        static string NombreSerie_SinIdentificar = "Sin Identificar";

        StringCollection Input_TxTfilePaths { get; set; }
        //static string MyPublicIP = GetOwnIP();
        static bool Get_Google_INFO = false;
        static int WaitTime = 500; // En ms, para evitar abusar de la API de Google y que nos bloqueen
        static string MyPublicIP = string.Empty;
        

        public Form1()
        {
            InitializeComponent();            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox_TxT_Path.Text = string.Empty;
            HTML_Final = string.Empty;
            if (Get_Google_INFO)
                MyPublicIP = GetExternalAddress().ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Buscar archivo TXT y añadir su path al textbox
            TxT_Browse();
        }

        private void textBox1_DragOver(object sender, DragEventArgs e)
        {
            Multiple_TxT_DragEnter(e);
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            TxT_DragDrop(e);
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            // Get file name.
            string Filename = saveFileDialog1.FileName;

            // Save File:
            File.WriteAllText(Filename, this.HTML_Final, UTF8Encoding.UTF8);

            // Mensaje en la barra de estado:
            toolStripStatusLabel1.Text = "HTML Generado con éxito";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Generar archvivo HTML a partir de los txt's con elinks

            if (Input_TxTfilePaths.Count > 0)
            {               

                // ----------------------------------------
                // Procesado en paralelo de todos los TxT's
                // ----------------------------------------

                try
                {

                    bool RunInParallel = true;
                    TXT_Procesado[] Array_TxT_Procesados = new TXT_Procesado[Input_TxTfilePaths.Count];
                    // ¿Mejor Blocking collection?

                    if (!RunInParallel)
                    {
                        for (int i = 0; i < Input_TxTfilePaths.Count; i++ )
                        {
                            TXT_Procesado Nuevo_TXT_Procesado = new TXT_Procesado();
                            Nuevo_TXT_Procesado.Path_TXT = Input_TxTfilePaths[i];
                            Nuevo_TXT_Procesado.Lista_Series = ParseTxT(Input_TxTfilePaths[i], Get_Google_INFO);
                            Array_TxT_Procesados[i] = Nuevo_TXT_Procesado;
                        }
                    }
                    else
                    {
                        Parallel.For(0, Input_TxTfilePaths.Count, i =>
                        {
                            TXT_Procesado Nuevo_TXT_Procesado = new TXT_Procesado();
                            Nuevo_TXT_Procesado.Path_TXT = Input_TxTfilePaths[i];
                            Nuevo_TXT_Procesado.Lista_Series = ParseTxT(Input_TxTfilePaths[i], Get_Google_INFO);
                            Array_TxT_Procesados[i] = Nuevo_TXT_Procesado;
                        });
                    }


                    // Fusionamos todos los objetos "Links_misma_Serie" y los ordenamos por orden alfabético
                    List<Links_misma_Serie> Mezcla_Resultados = Mezcla_Resultados_Series(Array_TxT_Procesados);


                    // Sin mezclar resultados, pero ordenandolos de forma alfabética:
                    Links_misma_Serie_sort_by_Name Sort_Method = new Links_misma_Serie_sort_by_Name();
                    for (int i = 0; i < Array_TxT_Procesados.Length; i++)
                        Array_TxT_Procesados[i].Lista_Series.Sort(Sort_Method);


                    // Separar por Letra Inicial (para el Menú y las agrupaciones)
                    List<Links_misma_Serie> Series_Empieza_con_Letra_o_Digito, Series_No_Empieza_con_Letra_o_Digito, Series_Sin_Identificar;
                    List<char> LetrasIniciales = Get_Initial_Letters(Mezcla_Resultados,
                                                                     out Series_Empieza_con_Letra_o_Digito,
                                                                     out Series_No_Empieza_con_Letra_o_Digito,
                                                                     out Series_Sin_Identificar);
                    Series_Empieza_con_Letra_o_Digito.Sort(Sort_Method); 
                    Series_No_Empieza_con_Letra_o_Digito.Sort(Sort_Method);
                    Series_Sin_Identificar.Sort(Sort_Method); 
                    

                    // Generamos el HTML final:
                    // ------------------------
                    // 1) El HTML Head:
                    string Title = "Recopilación de eLinks";
                    string author = "Kerensky";
                    string Head = Generate_HTML_Head(Title, Title, author);     
               
                    // 2) El Body:
                    string NombreRestoSeries = "Otras";
                    string Body = Generate_HTML_Body(LetrasIniciales, NombreRestoSeries,
                                                     Series_Empieza_con_Letra_o_Digito,
                                                     Series_No_Empieza_con_Letra_o_Digito,
                                                     Series_Sin_Identificar);
                    
                    // 3) HTML Final:
                    this.HTML_Final = Head + Body;


                    // Le preguntamos al user que donde lo quiere guardar
                    // --------------------------------------------------
                    // (empezar misma carpeta que el 1º de los TxT's)
                    if (!String.IsNullOrWhiteSpace(this.HTML_Final))
                    {
                        if (File.Exists(Input_TxTfilePaths[0]))
                            saveFileDialog1.InitialDirectory = Path.GetDirectoryName(Input_TxTfilePaths[0]);
                        else
                            saveFileDialog1.InitialDirectory = Application.StartupPath;

                        if (Input_TxTfilePaths.Count == 1)
                            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(Input_TxTfilePaths[0]);

                        // Pregunta al user dónde guardar el HTML
                        saveFileDialog1.ShowDialog(this);
                    }
                    else
                        MessageBox.Show("Fallo al generar el HTML", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                catch (Exception Excepcion)
                {
                    // Exceptions thrown by tasks will be propagated to the main thread 
                    string Error_MSG = "Error parseando un txt" + Environment.NewLine
                                        + Excepcion.Message + Environment.NewLine
                                        + Excepcion.InnerException;
                    MessageBox.Show(Error_MSG, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                MessageBox.Show("No se ha seleccionado ningún txt con elinks", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }




        #region GENERATE HTML

        private List<Links_misma_Serie> Mezcla_Resultados_Series(TXT_Procesado[] Array_TxT_Procesados)
        {
            List<Links_misma_Serie> All_Results = new List<Links_misma_Serie>();

            Links_misma_Serie SinIdentificar = new Links_misma_Serie();
            SinIdentificar.NombreSerie = NombreSerie_SinIdentificar;

            for (int i = 0; i < Array_TxT_Procesados.Length; i++)
            {
                for (int j = 0; j < Array_TxT_Procesados[i].Lista_Series.Count; j++)
                {
                    int index_nombre_existe;

                    if (Array_TxT_Procesados[i].Lista_Series[j].NombreSerie == NombreSerie_SinIdentificar)
                    {
                        SinIdentificar.eLinks_RAW.AddRange(Convert_SC_To_Array(Array_TxT_Procesados[i].Lista_Series[j].eLinks_RAW));
                        SinIdentificar.eLinks_decoded.AddRange(Convert_SC_To_Array(Array_TxT_Procesados[i].Lista_Series[j].eLinks_decoded));
                    }
                    else if (Already_Exists(All_Results, Array_TxT_Procesados[i].Lista_Series[j].NombreSerie, out index_nombre_existe))
                    {
                        // FUSIONAR TODOS LOS QUE TENGAN EL MISMO NOMBRE:
                        All_Results[index_nombre_existe].eLinks_RAW.AddRange(Convert_SC_To_Array(Array_TxT_Procesados[i].Lista_Series[j].eLinks_RAW));
                        All_Results[index_nombre_existe].eLinks_decoded.AddRange(Convert_SC_To_Array(Array_TxT_Procesados[i].Lista_Series[j].eLinks_decoded));
                    }
                    else
                        All_Results.Add(Array_TxT_Procesados[i].Lista_Series[j]);
                }
            }

            // Ordenamos por orden alfabético:
            Links_misma_Serie_sort_by_Name Sort_Method = new Links_misma_Serie_sort_by_Name();
            All_Results.Sort(Sort_Method);


            // Añadimos las que están sin nombre:
            if (SinIdentificar.eLinks_RAW.Count > 0)
                All_Results.Add(SinIdentificar);

            return All_Results;
        }

        class Links_misma_Serie_sort_by_Name : IComparer<Links_misma_Serie>
        {
            public int Compare(Links_misma_Serie x, Links_misma_Serie y)
            {
                return x.NombreSerie.CompareTo(y.NombreSerie);
            }
        }

        private string[] Convert_SC_To_Array (StringCollection Input)
        {
            String[] myArr = new String[Input.Count];
            Input.CopyTo( myArr, 0 );
            return myArr;
        }

        private bool Already_Exists(List<Links_misma_Serie> Results, string NombreSerie, out int index_nombre_existe)
        {
            index_nombre_existe = -1;
            bool Ya_esiste = false;

            for (int i=0; i < Results.Count; i++)
            {
                if (Results[i].NombreSerie.ToLowerInvariant() == NombreSerie.ToLowerInvariant())
                {
                    index_nombre_existe = i;
                    Ya_esiste = true;
                    break;
                }
            }

            return Ya_esiste;
        }

        private List<char> Get_Initial_Letters(List<Links_misma_Serie> Resultados,
            out List<Links_misma_Serie> Series_Empieza_con_Letra_o_Digito,
            out List<Links_misma_Serie> Series_No_Empieza_con_Letra_o_Digito,
            out List<Links_misma_Serie> Series_Sin_Identificar)
        {
            // Para el menú que lleva a las series que empiezan por esa letra

            List<char> Letras_Iniciales_Presentes = new List<char>();
            Series_No_Empieza_con_Letra_o_Digito = new List<Links_misma_Serie>();
            Series_Sin_Identificar = new List<Links_misma_Serie>();
            Series_Empieza_con_Letra_o_Digito = new List<Links_misma_Serie>();

            if (Resultados.Count > 0)
            {
                foreach(Links_misma_Serie Serie in Resultados)
                {
                    if (Serie.NombreSerie != NombreSerie_SinIdentificar)
                    {
                        char CaracterInicial = Serie.NombreSerie.ToUpperInvariant()[0];

                        if (char.IsLetterOrDigit(CaracterInicial))
                        {
                            Series_Empieza_con_Letra_o_Digito.Add(Serie);

                            if (!Letras_Iniciales_Presentes.Contains(CaracterInicial))
                                Letras_Iniciales_Presentes.Add(CaracterInicial);                            
                        }
                        else
                            Series_No_Empieza_con_Letra_o_Digito.Add(Serie);
                    }
                    else
                        Series_Sin_Identificar.Add(Serie);
                }
            }

            Letras_Iniciales_Presentes.Sort();

            return Letras_Iniciales_Presentes;
        }


        private string Generate_HTML_Head(string title, string description, string author)
        {
            return string.Format(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01//EN""      ""http://www.w3.org/TR/html4/strict.dtd"">
                                    <html>
                                    <title>{0}</title>
                                    <meta name=""description"" content={1}>
                                    <meta name=""author"" content={2}>
                                    </head>
                                    ", title, description, author);
        }


        private string Generate_HTML_Body(List<char> LetrasIniciales, string NombreRestoSeries,
                                          List<Links_misma_Serie> Series_Empieza_con_Letra_o_Digito,
                                          List<Links_misma_Serie> Series_No_Empieza_con_Letra_o_digito,
                                          List<Links_misma_Serie> Series_Sin_Identificar)
        {
            string Body = string.Empty;

            // Open Body Tag:
            Body += "<body>" + Environment.NewLine;


            // 1) El HTML Header (Menu Letras iniciales):
            // ----------------------------------------
            string Header = Generate_HTML_Menu(LetrasIniciales, NombreRestoSeries);
            Body += Header + Environment.NewLine;



            // 2) Generamos el listado de las series agrupadas:
            // ------------------------------------------------
            List<Links_misma_Serie> Series_Empiezan_por_Digito = new List<Links_misma_Serie>();

            // Secciones con La Letra inicial de id
            foreach(char LetraInicial in LetrasIniciales)
            {
                List<Links_misma_Serie> Temp_Series_Empiezan_por_esa_Letra = new List<Links_misma_Serie>();

                foreach (Links_misma_Serie Serie in Series_Empieza_con_Letra_o_Digito)
                {
                    if ((char.IsDigit(LetraInicial)) &&
                        (Serie.NombreSerie.StartsWith(LetraInicial.ToString()))
                        && (Serie.NombreSerie != NombreSerie_SinIdentificar))
                    {
                        Series_Empiezan_por_Digito.Add(Serie);
                    }
                    else if ((Serie.NombreSerie.StartsWith(LetraInicial.ToString()))
                        && (Serie.NombreSerie != NombreSerie_SinIdentificar))
                    {
                        Temp_Series_Empiezan_por_esa_Letra.Add(Serie);
                    }                    
                }

                if (Temp_Series_Empiezan_por_esa_Letra.Count > 0)
                {
                    string Temp_Seccion = Make_Letter_Section(LetraInicial.ToString(), Temp_Series_Empiezan_por_esa_Letra);
                    Body += Temp_Seccion;
                }                
            }

            string Empiezan_por_Digito = Make_Letter_Section("Numbers", Series_Empiezan_por_Digito);
            Body += Empiezan_por_Digito;

            string Otras_Series_Seccion = Make_Letter_Section(NombreRestoSeries, Series_No_Empieza_con_Letra_o_digito);
            Body += Otras_Series_Seccion;

            string Series_No_Identificadas = Make_Letter_Section(NombreSerie_SinIdentificar, Series_Sin_Identificar);
            Body += Series_No_Identificadas;

            // Close Body Tag:
            Body +=  Environment.NewLine+ "</body>";

            return Body;
        }

        private string Make_Letter_Section(string NombreSeccion, List<Links_misma_Serie> Series_Mismo_Conjunto)
        {
            StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                // <div id=<Letra Inicial> > // Style = Panel
                // <h1>Letra A</h1>
                //      <div id=<NombreSerie> >
                //      <h3>Nombre de la Serie</h3>
                //          <a href="eLink_RAW">eLink_decoded</a> [Lista eLinks]

                if ((NombreSeccion.Length == 1) && (char.IsLetter(NombreSeccion[0])))
                    NombreSeccion = "Letra " + NombreSeccion.ToUpperInvariant();
                else if ((NombreSeccion.Length == 1) && (char.IsNumber(NombreSeccion[0])))
                    NombreSeccion = "Número " + NombreSeccion.ToUpperInvariant();

                writer.AddAttribute(HtmlTextWriterAttribute.Id, NombreSeccion);
                writer.RenderBeginTag(HtmlTextWriterTag.Div); // Seccion Letra Inicial
                writer.RenderBeginTag(HtmlTextWriterTag.H1);
                writer.Write(NombreSeccion);
                writer.RenderEndTag(); // </h1>

                foreach (Links_misma_Serie Serie in Series_Mismo_Conjunto)
                {                    
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // Seccion Serie

                    if (!String.IsNullOrWhiteSpace(Serie.IMDB_Link))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, Serie.IMDB_Link);
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="IMDB_Link">
                    }

                    writer.RenderBeginTag(HtmlTextWriterTag.H3);
                    writer.Write(Serie.NombreSerie.ToUpperInvariant());
                    writer.RenderEndTag(); // </h3>

                    if (!String.IsNullOrWhiteSpace(Serie.IMDB_Link))
                        writer.RenderEndTag(); // </a> 

                    if (!String.IsNullOrWhiteSpace(Serie.Image_Link))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Src, Serie.Image_Link);
                        writer.AddAttribute(HtmlTextWriterAttribute.Width, "auto");
                        writer.AddAttribute(HtmlTextWriterAttribute.Height, "120");
                        writer.AddAttribute(HtmlTextWriterAttribute.Alt, Serie.NombreSerie);

                        writer.RenderBeginTag(HtmlTextWriterTag.Img); // 1ª Imagen del Google
                        writer.RenderEndTag(); // </img>
                    }
                    writer.WriteLine(); // Salto de Línea

                    writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                    writer.Indent++;
                    for (int i = 0; i < Serie.eLinks_RAW.Count; i++)
                    {
                        writer.Indent++;
                        writer.RenderBeginTag(HtmlTextWriterTag.Li);
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, Serie.eLinks_RAW[i]);
                        //writer.AddAttribute(HtmlTextWriterAttribute.Href, HttpUtility.UrlEncode(Serie.eLinks_RAW[i]));
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                        writer.Write(Format_Decoded_eLink(Serie.eLinks_decoded[i]));
                        writer.RenderEndTag(); // </a>   
                        writer.WriteLine(); // Salto de Línea
                        writer.RenderEndTag(); // </li> 
                        writer.Indent--;
                    }
                    writer.Indent--;
                    writer.RenderEndTag(); // </ul>  

                    writer.RenderEndTag(); // </div> [Del nombre de la Serie]     
                    writer.WriteLine(); // Salto de Línea

                    //string Debug = stringWriter.ToString();
                }

                writer.RenderEndTag(); // </div> [La letra inicial]
                writer.WriteLine(); // Salto de Línea
            }
            // Return the result.
            return stringWriter.ToString();
        }


        private string Generate_HTML_Menu(List<char> LetrasInicial, string NombreRestoSeries)
        {
            // MENU para el acceso directo -> Letras Iniciales + "[0-9]" + "Otras" + "Sin Identificar"

            StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                // <div id=MENU> // Style = Top-bar
                // <h2>
                // <a href="Letra A">A</a>
                // ...
                // </h2>

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "MENU");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.WriteLine(); // Salto de Línea
                writer.RenderBeginTag(HtmlTextWriterTag.H2);

                writer.Indent++;
                foreach(char Letra in LetrasInicial)
                {
                    if (char.IsLetter(Letra))
                    {
                        string URL_Letra = "#Letra " + Letra.ToString().ToUpperInvariant();
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Letra);
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                        writer.Write(Letra.ToString().ToUpperInvariant() + "  ");
                        writer.RenderEndTag(); // </a>   
                    }                    
                }

                string URL_Numbers = "#Numbers";
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Numbers);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("[0-9]  ");
                writer.RenderEndTag(); // </a>

                string URL_Otras = "#" + NombreRestoSeries;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Otras);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("Otras  ");
                writer.RenderEndTag(); // </a>

                string URL_SinIdentificar = "#" + NombreSerie_SinIdentificar;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_SinIdentificar);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write(NombreSerie_SinIdentificar);
                writer.RenderEndTag(); // </a>

                writer.Indent--;

                writer.RenderEndTag(); // </h2>
            }
            // Return the result.
            return stringWriter.ToString();
        }

        #endregion


        #region Parse TxT con Elinks

        private static List<Links_misma_Serie> ParseTxT(string TxTpath, bool Get_Google_INFO)
        {
            List<Links_misma_Serie> Lista_Series = new List<Links_misma_Serie>();

            if (File.Exists(TxTpath))
            {
                string[] TxT_Contenido = File.ReadAllLines(TxTpath);

                Links_misma_Serie SinIdentificar = new Links_misma_Serie();
                SinIdentificar.NombreSerie = NombreSerie_SinIdentificar;
                int LongitudTextoComunMinima = 4; // Nº caracteres comunes con el siguiente para considerarse de la misma serie

                for (int i = 0; i < TxT_Contenido.Length; i++)
                {

                    // 1) URL Decode para tener el nombre bien
                    string eLink_RAW = TxT_Contenido[i];
                    string eLink_decoded = HttpUtility.UrlDecode(eLink_RAW);

                    // 2) Nombre de la serie:
                    eLink_decoded = eLink_decoded.Replace(".", " ").Replace("_", " ");
                    string NombreSerie = GetNombreSerie(eLink_decoded);

                    // Si no se dectecto el nombre buscando un patrón (Serie+Episodio), se busca el texto que se va repitiendo:
                    if ( (String.IsNullOrWhiteSpace(NombreSerie)) && (i < TxT_Contenido.Length - 1) )
                    {
                        string eLink_decoded_siguiente = HttpUtility.UrlDecode(TxT_Contenido[i+1]);
                        NombreSerie = GetNombreSerieSiSeRepite(eLink_decoded, eLink_decoded_siguiente, LongitudTextoComunMinima);
                    }

                    
                    if ( (Lista_Series.Count == 0) && (!String.IsNullOrEmpty(NombreSerie)) )
                    {
                        // Si el nombre la serie es distinto al anterior (o lista vacía) --> Nuevo item de la lista
                        Links_misma_Serie NuevaSerie = new Links_misma_Serie();
                        NuevaSerie.NombreSerie = NombreSerie;
                        NuevaSerie.eLinks_RAW.Add(eLink_RAW);
                        NuevaSerie.eLinks_decoded.Add(eLink_decoded);
                        if (Get_Google_INFO)
                            Lista_Series.Add(Get_Info_Nueva_Serie(NuevaSerie));
                        else
                            Lista_Series.Add(NuevaSerie);
                    }
                    else if ( (Lista_Series.Count == 0) && (String.IsNullOrEmpty(NombreSerie)) )
                    {
                        // Si lista vacía pero no conseguimos identificar el nombre de la serie --> Añadir a eLinks sin identificar
                        SinIdentificar.eLinks_RAW.Add(eLink_RAW);
                        SinIdentificar.eLinks_decoded.Add(eLink_decoded);
                    }
                    else if ((!String.IsNullOrEmpty(NombreSerie))
                        && (String.Compare(NombreSerie.ToLowerInvariant(), Lista_Series.Last().NombreSerie.ToLowerInvariant(), true)) != 0)
                    {
                        // Si se reconoce el Nombre de la serie y es distinto al anterior --> Nuevo item en la lista
                        Links_misma_Serie NuevaSerie = new Links_misma_Serie();
                        NuevaSerie.NombreSerie = NombreSerie;
                        NuevaSerie.eLinks_RAW.Add(eLink_RAW);
                        NuevaSerie.eLinks_decoded.Add(eLink_decoded);
                        if (Get_Google_INFO)
                            Lista_Series.Add(Get_Info_Nueva_Serie(NuevaSerie));
                        else
                            Lista_Series.Add(NuevaSerie);
                    }
                    else if ((!String.IsNullOrEmpty(NombreSerie))
                    && (String.Compare(NombreSerie.ToLowerInvariant(), Lista_Series.Last().NombreSerie.ToLowerInvariant(), true)) == 0)
                    {
                        // Si se reconoce el Nombre de la serie y es igual al anterior
                        // --> Añadir al último elemento de la lista
                        Lista_Series.Last().eLinks_RAW.Add(eLink_RAW);
                        Lista_Series.Last().eLinks_decoded.Add(eLink_decoded);
                    }
                    else if ((String.IsNullOrEmpty(NombreSerie))
                        && (Format_Decoded_eLink(eLink_decoded).ToLowerInvariant().Contains(Lista_Series.Last().NombreSerie.ToLowerInvariant())))
                    {
                        // Si no se reconoció el nombre de la serie pero empieza el eLink por el mismo Nombre de la Serie que el eLink anterior
                        // --> Añadir al último elemento de la lista
                        Lista_Series.Last().eLinks_RAW.Add(eLink_RAW);
                        Lista_Series.Last().eLinks_decoded.Add(eLink_decoded);
                    }
                    else
                    {
                        // Se añade a Sin Identificar
                        SinIdentificar.eLinks_RAW.Add(eLink_RAW);
                        SinIdentificar.eLinks_decoded.Add(eLink_decoded);
                    }     
               
                } // end for

                if (SinIdentificar.eLinks_RAW.Count > 0)
                {
                    // Añadimos los eLinks sin identificar a la lista:
                    Lista_Series.Add(SinIdentificar);
                }
            }

            return Lista_Series;
        }

        private static string GetNombreSerie(string input)
        {
            string NombreSerie = string.Empty;
            input = Format_Decoded_eLink(input);
            //input = input.ToLowerInvariant();
            input = EliminaTextoEntreCorchetesParentesis(input);


            // Regex buscando la cadena:
            // -------------------------
            string Pattern = "[0-9]{1,3}(X|x)[0-9]{1,3}";
            string Pattern2 = "(S|s)[0-9]{1,3}(E|e)[0-9]{1,3}";
            string Pattern3 = "(Temporada|temporada)[0-9]{1,3}";
            string Pattern4 = "(Episodio|episodio)[0-9]{0,3}";
            string Pattern5 = "(Episode|episode)[0-9]{0,3}";
            string Pattern6 = "(Capitulo|capitulo|Capítulo|capítulo)[0-9]{0,3}";
            string Pattern7 = "(Parte|parte)[0-9]{0,3}";
            string Pattern8 = "(Ep|ep)[0-9]{0,3}";
            string Pattern9 = "(E|e)[0-9]{1,3}";
            string Pattern10 = "(Cd|cd|Cd |cd )[0-9]{1,3}";

            Match match = Regex.Match(input, Pattern, RegexOptions.CultureInvariant);

            if (match.Success)
                NombreSerie = Formate_Nombre_Series(input, match.Index);

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                // Probamos usando el patrón: S<Numero>E<Numero>
                match = Regex.Match(input, Pattern2, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern3, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern4, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern5, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern6, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern7, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern8, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern9, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }

            if (String.IsNullOrWhiteSpace(NombreSerie))
            {
                match = Regex.Match(input, Pattern10, RegexOptions.CultureInvariant);
                if (match.Success)
                    NombreSerie = Formate_Nombre_Series(input, match.Index);
            }


            return NombreSerie;
        }

        private static string Formate_Nombre_Series(string input, int Pos_comienzo_patron)
        {
            if ((Pos_comienzo_patron > 0) && (Pos_comienzo_patron < input.Length))
            {
                string Temp = input.Substring(0, Pos_comienzo_patron).Trim();

                if (!Temp.Contains("|"))
                    return Temp;
                else
                    return string.Empty;
            }
            else
                return string.Empty;
        }

        private static string Format_Decoded_eLink(string eLink_decoded)
        {
            string Protocolo = "ed2k://|file|";
            eLink_decoded = eLink_decoded.Replace(Protocolo, string.Empty);

            char Separador = '|';
            int Pos_1er_separador = eLink_decoded.IndexOf(Separador);
            if (Pos_1er_separador > 0)
                eLink_decoded = eLink_decoded.Substring(0, Pos_1er_separador);

            return eLink_decoded.Trim();
        }

        private static string GetNombreSerieSiSeRepite(string input_actual, string input_siguiente, int LongitudTextoComunMinima)
        {
            string NombreSerie = string.Empty;
            input_actual = Format_Decoded_eLink(input_actual).Replace("-", string.Empty).Trim();
            input_siguiente = Format_Decoded_eLink(input_siguiente).Replace("-", string.Empty).Trim();

            // Eliminamos el texto entre parentesis o corchetes:
            input_actual = EliminaTextoEntreCorchetesParentesis(input_actual);
            input_siguiente = EliminaTextoEntreCorchetesParentesis(input_siguiente);

            int Longitud_TextoInicial_en_comun = 0;

            while ((Longitud_TextoInicial_en_comun < input_actual.Length)
                    && (Longitud_TextoInicial_en_comun < input_siguiente.Length)
                    && (input_actual.ToLowerInvariant()[Longitud_TextoInicial_en_comun] == input_siguiente.ToLowerInvariant()[Longitud_TextoInicial_en_comun]))

            {
                Longitud_TextoInicial_en_comun++;
            }

            if (Longitud_TextoInicial_en_comun >= LongitudTextoComunMinima)
            {
                NombreSerie = input_actual.Substring(0, Longitud_TextoInicial_en_comun);
                NombreSerie = NombreSerie.Replace("-", string.Empty).Trim();
            }

            return NombreSerie;
        }

        private static string EliminaTextoEntreCorchetesParentesis(string input)
        {
            // Eliminamos el texto entre parentesis o corchetes:
            string regex = "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))";
            return Regex.Replace(input, regex, string.Empty).Trim();
        }


        #region GOOGLE INFO

        private static Links_misma_Serie Get_Info_Nueva_Serie(Links_misma_Serie NuevaSerie)
        {
            // 4) Si se detecto un nuevo nombre de serie: Buscar IMDB Link y una Imagen
            // ------------------------------------------------------------------------
                        
            using (var webClient = new System.Net.WebClient())
            {
                Thread.CurrentThread.Join(WaitTime); 

                // IMDB LINK:
                // ---------
                string URL_Google_Search_Name = "https://ajax.googleapis.com/ajax/services/search/web?v=1.0&userip=" + MyPublicIP + "&q=" + NuevaSerie.NombreSerie + " TV Series imdb";
                string json_response = webClient.DownloadString(URL_Google_Search_Name);
                // Parse with JSON.Net
                NuevaSerie.IMDB_Link = GetURLfromGoogleJSON(json_response, false);

                Thread.CurrentThread.Join(WaitTime); // 100ms para evitar abusar de la API de Google y que nos bloqueen

                // IMAGEN DE LA SERIE:
                // ------------------
                string URL_Google_Search_Image = "https://ajax.googleapis.com/ajax/services/search/images?v=1.0&userip=" + MyPublicIP + "&q=" + NuevaSerie.NombreSerie + " TV Series";
                string json_response2 = webClient.DownloadString(URL_Google_Search_Image);
                // Parse with JSON.Net
                NuevaSerie.Image_Link = GetURLfromGoogleJSON(json_response2, true);
            }

            return NuevaSerie;
        }

        private static string GetURLfromGoogleJSON(string GoogleJson, bool isImage)
        {
            // Parse with JSON.Net
            /*
             * DESACTIVADO EL CÓDIGO Y ELIMINADA LA REFERENCIA AL Json.NET dll
             * por las limitaciones de la APIs de Google
             * 

            try
            {
                var jObj = (JObject)JsonConvert.DeserializeObject(GoogleJson);
                string[] urls = jObj["responseData"]["results"]
                                .Select(x => (string)x["url"])
                                .ToArray();

                if (urls.Length > 0)
                {
                    if (isImage)
                    {
                        foreach (string url in urls)
                        {
                            // Nos quedamos con la 1ª web/imagen que no devuelva un error:
                            if (RemoteFileExists(url))
                                return url;
                        }
                    }
                    else
                        return urls[0];
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Se ha excedido el límite de peticiones a la API de google");
                Get_Google_INFO = false;
            }
            */

            return string.Empty;
        }

        ///
        /// Checks the file exists or not.
        ///
        /// The URL of the remote file.
        /// True : If the file exits, False if file not exists
        private static bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 300; // En milisegundos
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }

        private static string GetOwnIP()
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            var ip = (
                       from addr in hostEntry.AddressList
                       where addr.AddressFamily.ToString() == "InterNetwork"
                       select addr.ToString()
                ).FirstOrDefault();

            return ip.Trim();
        }

        ///<summary>Gets the computer's external IP address from the internet.</summary>
        static IPAddress GetExternalAddress()
        {
            //<html><head><title>Current IP Check</title></head><body>Current IP Address: 129.98.193.226</body></html>
            var html = new WebClient().DownloadString("http://checkip.dyndns.com/");

            var ipStart = html.IndexOf(": ", StringComparison.OrdinalIgnoreCase) + 2;
            return IPAddress.Parse(html.Substring(ipStart, html.IndexOf("</", ipStart, StringComparison.OrdinalIgnoreCase) - ipStart));
        }

        #endregion
        
        #endregion



        #region UTILS

        private void Multiple_TxT_DragEnter(DragEventArgs e)
        {
            // Check for files in the hovering data object.
            StringCollection fileNames = Get_All_FileNames(e);

            bool All_TxT_Files = true;
            foreach (string NombreFileDrag in fileNames)
            {
                string ExtensionFileDrag = Path.GetExtension(NombreFileDrag);

                if ( (!String.IsNullOrEmpty(NombreFileDrag)) && (ExtensionFileDrag != ".txt") )
                    All_TxT_Files = false;
            }

            if (All_TxT_Files)
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void TxT_DragDrop(DragEventArgs e)
        {
            // Check for files in the hovering data object.
            StringCollection fileNames = Get_All_FileNames(e);
            Input_TxTfilePaths = new StringCollection();

            foreach (string NombreFileDrag in fileNames)
            {
                string ExtensionFileDrag = Path.GetExtension(NombreFileDrag);

                if ((!String.IsNullOrEmpty(NombreFileDrag)) && (ExtensionFileDrag == ".txt"))
                {
                    textBox_TxT_Path.Text += Path.GetFileNameWithoutExtension(NombreFileDrag) + Environment.NewLine;
                    Input_TxTfilePaths.Add(NombreFileDrag);
                }
            }

            CheckExistsInputs();
        }

        private void TxT_Browse()
        {
            openFileDialog1.InitialDirectory = Application.StartupPath;

            Input_TxTfilePaths = new StringCollection();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach(string TxT in openFileDialog1.FileNames)
                {
                    textBox_TxT_Path.Text += Path.GetFileNameWithoutExtension(TxT) + Environment.NewLine;
                    Input_TxTfilePaths.Add(TxT);
                }

                CheckExistsInputs();
            }
        }
        
        private StringCollection Get_All_FileNames(DragEventArgs e)
        {
            StringCollection FileNames = new StringCollection();

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                FileNames.AddRange(e.Data.GetData(DataFormats.FileDrop, true) as string[]);
            }

            return FileNames;
        }


        private void CheckExistsInputs()
        {
            if (this.Input_TxTfilePaths.Count > 0)
            {
                this.button2.Enabled = true;
                this.toolStripStatusLabel1.Text = "Listo para intentar generar el HTML";
            }
        }

        #endregion

        

        

        
    }
}
