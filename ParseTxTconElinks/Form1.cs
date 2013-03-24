using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO.Compression;
using ParseTxTconElinks.Properties;
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
        string HTML_Final_Bootstrap { get; set; }
        bool Generar_HTML_Texto_Plano = false;
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
            HTML_Final_Bootstrap = string.Empty;
            if (Get_Google_INFO)
                MyPublicIP = GetExternalAddress().ToString();

            toolStripMenuItem_HTML_TextoPlano.Checked = Generar_HTML_Texto_Plano;
            toolStripMenuItem_HTML_New.Checked = !Generar_HTML_Texto_Plano;
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

            if (!Generar_HTML_Texto_Plano)
            {
                Descomprimir_Assets(Path.GetDirectoryName(Filename));
            }

            // Mensaje en la barra de estado:
            toolStripStatusLabel1.Text = "HTML Generado con éxito";
        }

        private void toolStripMenuItem_HTML_New_Click(object sender, EventArgs e)
        {            
            if (toolStripMenuItem_HTML_New.Checked)
                toolStripMenuItem_HTML_TextoPlano.Checked = false;
            else
                toolStripMenuItem_HTML_TextoPlano.Checked = true;

            Generar_HTML_Texto_Plano = toolStripMenuItem_HTML_TextoPlano.Checked;
        }

        private void toolStripMenuItem_HTML_TextoPlano_Click(object sender, EventArgs e)
        {            
            if (toolStripMenuItem_HTML_TextoPlano.Checked)
                toolStripMenuItem_HTML_New.Checked = false;
            else
                toolStripMenuItem_HTML_New.Checked = true;

            Generar_HTML_Texto_Plano = toolStripMenuItem_HTML_TextoPlano.Checked;
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
                    string Head = string.Empty;
                    if (Generar_HTML_Texto_Plano)
                        Head = Generate_HTML_Head_TextoPlano(Title, Title, author);
                    else
                        Head = Generate_HTML_Head_Bootstrap(Title, Title, author);
               
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
                    string Error_MSG = "Error parseando un txt" + Environment.NewLine + Environment.NewLine
                                        + Excepcion.Message + Environment.NewLine + Environment.NewLine
                                        + Excepcion.InnerException + Environment.NewLine + Environment.NewLine
                                        + Excepcion.StackTrace;
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
                if (Results[i].NombreSerie.Trim().ToLowerInvariant() == NombreSerie.Trim().ToLowerInvariant())
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


        private string Generate_HTML_Head_TextoPlano(string title, string description, string author)
        {
            return string.Format(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01//EN""      ""http://www.w3.org/TR/html4/strict.dtd"">
                                    <html>
                                    <title>{0}</title>
                                    <meta name=""description"" content={1}>
                                    <meta name=""author"" content={2}>
                                    </head>
                                    ", title, description, author);
        }

        private string Generate_HTML_Head_Bootstrap(string title, string description, string author)
        {
            string Head = @"<!DOCTYPE html>
                            <html lang=""es"">
                                <head>
                                <meta charset=""utf-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                                <title>" + title + @"</title>
	                            <meta name=""description"" content=" + description + @">
	                            <meta name=""author"" content=" + author + @">
                                <link href=""assets/bootstrap.min.css"" rel=""stylesheet"">
                                <style type=""text/css"">
                                    body {
                                    padding-top: 60px;
                                    padding-bottom: 40px;
                                    }
                                    .sidebar-nav {
                                    padding: 9px 0;
                                    }
	                                .fuente-peque ul{
	                                font-size: 10px;
		                            margin-left: 4px;
	                                }
                                    @media (max-width: 980px) {
                                    .navbar-text.pull-right {
                                        float: none;
                                        padding-left: 5px;
                                        padding-right: 5px;
                                    }
                                    }
	                                .sidebar-nav-fixed {
			                            position:fixed;
			                            top:44px;
			                            width:21.97%;
			                            padding: 9px 0;
		                            }
		                            @media (max-width: 767px) {
			                            .sidebar-nav-fixed {
				                            position:static;
				                            width:auto;
			                            }
		                            }
		                            @media (max-width: 979px) {
			                            .sidebar-nav-fixed {
				                            top:70px;
			                            }
		                            }
                                </style>
                                <link href=""assets/bootstrap-responsive.min.css"" rel=""stylesheet"">

                                <!-- HTML5 shim, for IE6-8 support of HTML5 elements -->
                                <!--[if lt IE 9]>
                                    <script src=""assets/html5shiv.js""></script>
                                <![endif]-->
                            </head>";
            return Head;
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
            string Header = string.Empty;
            if (this.Generar_HTML_Texto_Plano)
                Header = Generate_HTML_Menu(LetrasIniciales, NombreRestoSeries);
            else
            {
                string Header_Start = Header_Bootstrap_Start();

                string NavBar = Generate_HTML_Bootstrap_Menu_DropDown(LetrasIniciales, NombreRestoSeries,
                                                                      Series_Empieza_con_Letra_o_Digito,
                                                                      Series_No_Empieza_con_Letra_o_digito,
                                                                      Series_Sin_Identificar);

                string Header_Intermedio = Header_Bootstrap_Intermedio();

                string SideBar = Generate_HTML_Bootstrap_Menu_Sidebar(LetrasIniciales, NombreRestoSeries,
                                                                      Series_Empieza_con_Letra_o_Digito,
                                                                      Series_No_Empieza_con_Letra_o_digito,
                                                                      Series_Sin_Identificar);

                string Header_Final = "</div><!--/.well -->" + Environment.NewLine + "</div><!--/span-->";

                Header = Header_Start + NavBar + Header_Intermedio + SideBar + Header_Final;
            }

            Body += Header + Environment.NewLine;


            // 2) Generamos el listado de las series agrupadas:
            // ------------------------------------------------

            if (!this.Generar_HTML_Texto_Plano)
            {
                Body += @"<div class=""span9"" id=""listado-series"">";
                Body += Generate_HTML_Menu(LetrasIniciales, NombreRestoSeries);
            }
            

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


            if (!this.Generar_HTML_Texto_Plano)
                Body += Body_End_Bootstrap();


            // Close Body Tag:
            Body += Environment.NewLine+ "</body>";

            return Body;
        }

        private string Header_Bootstrap_Start()
        {
            return @"
                    <div class=""navbar navbar-fixed-top"">
                      <div class=""navbar-inner"">
                        <div class=""container-fluid"">
                          <button type=""button"" class=""btn btn-navbar"" data-toggle=""collapse"" data-target="".nav-collapse"">
                            <span class=""icon-bar""></span>
                            <span class=""icon-bar""></span>
                            <span class=""icon-bar""></span>
                          </button>
                          <a class=""brand"" href=""https://github.com/Kerensky25/ParseTxTeLinks"">ParseTxTeLinks</a>
                          <div class=""nav-collapse collapse"">
                            <p class=""navbar-text pull-right"">
                              by Kerensky
                            </p>
                            <ul class=""nav small""> 
				                <li class=""divider-vertical""></li>			
				                <li><p class=""navbar-text"">CTRL+F para activar el Buscador</p></li>		
				                <li class=""divider-vertical""></li>			
				                <li class=""dropdown"">
                                  <a href=""#"" class=""dropdown-toggle"" data-toggle=""dropdown"">SERIES <b class=""caret""></b></a>
                    ";
        }

        private string Header_Bootstrap_Intermedio()
        {
            return @"
                                </li>
				                <li class=""divider-vertical""></li>	
				                <li><button class=""btn"" id=""boton-mostrar-sidebar"" onclick=""if ($('#menu-series-lateral').is(':visible')){$('#menu-series-lateral').hide('slow'); $('#menu-series-lateral').attr('class', 'span1'); $('#listado-series').attr('class', 'span11')} else {$('#listado-series').attr('class', 'span9'); $('#menu-series-lateral').attr('class', 'span3'); $('#menu-series-lateral').show('slow')}"">Mostrar/Ocultar Menu Lateral</button></li>					
                            </ul>
                            </div><!--/.nav-collapse -->
                        </div>
                        </div>
                    </div>

                    <div class=""container-fluid"">
                        <div class=""row-fluid"">
	  
                        <div class=""span3"" id=""menu-series-lateral"">
                            <div class=""well sidebar-nav-fixed"">
                    ";
        }

        private string Body_End_Bootstrap()
        {
            return @"
                </div><!--/span-->		
                  </div><!--/row-->
                  <hr>
                </div><!--/.fluid-container-->

                <!-- javascript
                ================================================== -->
                <!-- Placed at the end of the document so the pages load faster -->
                <script src=""assets/jquery.js""></script>
                <script src=""assets/bootstrap.min.js""></script>
	
	            <script type=""text/javascript"">

	                $(document).ready(function() {

                        $('#menu-series-lateral').hide();

		                $(""#my-collapse-nav > li > a[data-target]"").parent('li').hover(
			                function() { 
				                target = $(this).children('a[data-target]').data('target');
				                $(target).collapse('show') 
			                },
			                function() { 
				                target = $(this).children('a[data-target]').data('target'); 
				                $(target).collapse('hide');
			                }
		                );
	                });

	            </script>
                ";
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
                    writer.AddAttribute(HtmlTextWriterAttribute.Id, Serie.NombreSerie.ToUpperInvariant());
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


        private string Generate_HTML_Menu(List<char> LetrasIniciales, string NombreRestoSeries)
        {
            // MENU para el acceso directo -> Letras Iniciales + "[0-9]" + "Otras"(NombreRestoSeries)  + "Sin Identificar"

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
                foreach (char Letra in LetrasIniciales)
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

        private string Generate_HTML_Bootstrap_Menu_DropDown(List<char> LetrasIniciales, string NombreRestoSeries,
                                                             List<Links_misma_Serie> Series_Empieza_con_Letra_o_Digito,
                                                             List<Links_misma_Serie> Series_No_Empieza_con_Letra_o_digito,
                                                             List<Links_misma_Serie> Series_Sin_Identificar)
        {
            // MENU para el acceso directo -> Letras Iniciales + "[0-9]" + "Otras"(NombreRestoSeries) + "Sin Identificar"
            //
            //<ul class="dropdown-menu">
            //    <li class="dropdown-submenu">
            //        <a href="#">A</a>
            //        <ul class="dropdown-menu">
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //        </ul>
            //    </li>
            //    <li class="dropdown-submenu">
            //        <a href="#">B</a>
            //        <ul class="dropdown-menu">
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //            <li><a href="#">Second level link</a></li>
            //        </ul>
            //    </li>
            //</ul>

            StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-menu");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;
                
                // -------------------------------------------------------------------------------
                // ------------------------------------------------------------------------------


                // ----------------
                //      LETRAS
                // ----------------

                foreach (char Letra in LetrasIniciales)
                {
                    if (char.IsLetter(Letra))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-submenu");
                        writer.RenderBeginTag(HtmlTextWriterTag.Li);
                        writer.Indent++;


                        string URL_Letra = "#Letra " + Letra.ToString().ToUpperInvariant();
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Letra);
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                        writer.Write(Letra.ToString().ToUpperInvariant());
                        writer.RenderEndTag(); // </a>   


                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-menu");
                        writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                        writer.Indent++;

                        writer.Write(Genera_Lista_items_ul_NombreSeries(Letra.ToString(), Series_Empieza_con_Letra_o_Digito));

                        writer.Indent--;
                        writer.RenderEndTag(); // </ul>


                        writer.Indent--;
                        writer.RenderEndTag(); // </li>
                    }
                }



                // ----------------
                //      NÚMEROS
                // ----------------
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-submenu");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;


                string URL_Numbers = "#Numbers";
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Numbers);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("[0-9]");
                writer.RenderEndTag(); // </a> 


                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-menu");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;

                writer.Write(Genera_Lista_items_ul_NombreSeries("Numbers", Series_Empieza_con_Letra_o_Digito));

                writer.Indent--;
                writer.RenderEndTag(); // </ul>


                writer.Indent--;
                writer.RenderEndTag(); // </li>




                // ----------------
                //      OTRAS
                // ----------------
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-submenu");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;
                
                string URL_Otras = "#" + NombreRestoSeries;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Otras);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("Otras");
                writer.RenderEndTag(); // </a> 


                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-menu");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;

                writer.Write(Genera_Lista_items_ul_NombreSeries(NombreRestoSeries, Series_No_Empieza_con_Letra_o_digito));

                writer.Indent--;
                writer.RenderEndTag(); // </ul>


                writer.Indent--;
                writer.RenderEndTag(); // </li>




                // --------------------------
                //      SIN IDENTIFICAR
                // --------------------------
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "dropdown-submenu");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;
                
                string URL_SinIdentificar = "#" + NombreSerie_SinIdentificar;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_SinIdentificar);
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write(NombreSerie_SinIdentificar);
                writer.RenderEndTag(); // </a>

                writer.Indent--;
                writer.RenderEndTag(); // </li>


                // -------------------------------------------------------------------------------
                // -------------------------------------------------------------------------------


                writer.Indent--;
                writer.RenderEndTag(); // </ul>
            }
            // Return the result.
            return stringWriter.ToString();
        }

        private string Generate_HTML_Bootstrap_Menu_Sidebar(List<char> LetrasIniciales, string NombreRestoSeries,
                                                            List<Links_misma_Serie> Series_Empieza_con_Letra_o_Digito,
                                                            List<Links_misma_Serie> Series_No_Empieza_con_Letra_o_digito,
                                                            List<Links_misma_Serie> Series_Sin_Identificar)
        {
            //<ul class="nav nav-list fuente-peque" id="my-collapse-nav">  
            //    <li class="nav-header">SERIES</li>  
            //    <li class="divider"></li>
            //    <li><a href="#" data-target="#demo">A</a> 
            //      <ul id="demo" class="collapse" style="height: 0px;">
            //          <li><a href="#">Second level link</a></li>
            //          <li><a href="#">Second level link</a></li>
            //          <li><a href="#">Second level link</a></li>
            //       </ul>
            //    </li>  
            //    <li><a href="#" data-target="#demo2">B</a></li> 
            //    <ul id="demo2" class="collapse" style="height: 0px;">
            //      <li><a href="#">Second level link</a></li>
            //      <li><a href="#">Second level link</a></li>
            //      <li><a href="#">Second level link</a></li>
            //    </ul>
            //</ul>

           StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "nav nav-list fuente-peque");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "my-collapse-nav");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;

                // ------------------------------------------------------------------------------

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "nav-header");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Write("SERIES");
                writer.RenderEndTag(); // </li>

                // ------------------------------------------------------------------------------

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "divider");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.RenderEndTag(); // </li>

                // -------------------------------------------------------------------------------
                // -------------------------------------------------------------------------------

                // ----------------
                //      LETRAS
                // ----------------
                int counter = 1;

                foreach (char Letra in LetrasIniciales)
                {
                    if (char.IsLetter(Letra))
                    {
                        // <li><a href="#" data-target="#demo">A</a> 
                        writer.RenderBeginTag(HtmlTextWriterTag.Li);
                        writer.Indent++;


                        // <a href="#" data-target="#demo">A</a>
                        string URL_Letra = "#Letra " + Letra.ToString().ToUpperInvariant();
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Letra);
                        writer.AddAttribute("data-target", "#demo" + counter.ToString());
                        
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                        writer.Write(Letra.ToString().ToUpperInvariant());
                        writer.RenderEndTag(); // </a>   

                        //<ul id="demo" class="collapse" style="height: 0px;">
                        writer.AddAttribute(HtmlTextWriterAttribute.Id, "demo" + counter.ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "collapse");
                        writer.AddAttribute(HtmlTextWriterAttribute.Style, "height: 0px;");
                        writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                        writer.Indent++;

                        writer.Write(Genera_Lista_items_ul_NombreSeries(Letra.ToString(), Series_Empieza_con_Letra_o_Digito));

                        writer.Indent--;
                        writer.RenderEndTag(); // </ul>
                        

                        writer.Indent--;
                        writer.RenderEndTag(); // </li>

                        counter++;
                    }
                }
                


                // ----------------
                //      NÚMEROS
                // ----------------
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;

                string URL_Numbers = "#Numbers";
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Numbers);
                writer.AddAttribute("data-target", "#demo" + counter.ToString());
                
                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("[0-9]");
                writer.RenderEndTag(); // </a>

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "demo" + counter.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "collapse");
                writer.AddAttribute(HtmlTextWriterAttribute.Style, "height: 0px;");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;

                writer.Write(Genera_Lista_items_ul_NombreSeries("Numbers", Series_Empieza_con_Letra_o_Digito));

                writer.Indent--;
                writer.RenderEndTag(); // </ul>


                writer.Indent--;
                writer.RenderEndTag(); // </li>

                counter++;



                // ----------------
                //      OTRAS
                // ----------------
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;

                string URL_Otras = "#" + NombreRestoSeries;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_Otras);
                writer.AddAttribute("data-target", "#demo" + counter.ToString());

                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write("Otras");
                writer.RenderEndTag(); // </a>

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "demo" + counter.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "collapse");
                writer.AddAttribute(HtmlTextWriterAttribute.Style, "height: 0px;");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                writer.Indent++;

                writer.Write(Genera_Lista_items_ul_NombreSeries(NombreRestoSeries, Series_No_Empieza_con_Letra_o_digito));


                writer.Indent--;
                writer.RenderEndTag(); // </li>

                counter++;




                // --------------------------
                //      SIN IDENTIFICAR
                // --------------------------
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.Indent++;

                string URL_SinIdentificar = "#" + NombreSerie_SinIdentificar;
                writer.AddAttribute(HtmlTextWriterAttribute.Href, URL_SinIdentificar);
                writer.AddAttribute("data-target", "#demo" + counter.ToString());

                writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                writer.Write(NombreSerie_SinIdentificar);
                writer.RenderEndTag(); // </a>

                writer.Indent--;
                writer.RenderEndTag(); // </li>

                counter++;


                // -------------------------------------------------------------------------------
                // -------------------------------------------------------------------------------

                writer.Indent--;
                writer.RenderEndTag(); // </ul>
            }
            // Return the result.
            return stringWriter.ToString();
        }


        private string Genera_Lista_items_ul_NombreSeries(string Texto_Inicial, List<Links_misma_Serie> Series)
        {
            //<li><a href="#">[Conjunto 1]Nombre Serie</a></li>
            //<li><a href="#">[Conjunto 1]Nombre Serie</a></li>
            //<li><a href="#">[Conjunto 1]Nombre Serie</a></li>

            StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                foreach (Links_misma_Serie Serie in Series)
                {
                    string SeriesName = Serie.NombreSerie;
                    string href_SeriesName = "#" + Serie.NombreSerie.ToUpperInvariant();
                    
                    if ( ( (Texto_Inicial == "Numbers") && (char.IsDigit(SeriesName[0])) )
                        || ((Texto_Inicial.Length == 1) && (char.IsLetter(SeriesName[0])) && (SeriesName.StartsWith(Texto_Inicial, StringComparison.InvariantCultureIgnoreCase)))
                        || ((SeriesName == NombreSerie_SinIdentificar) && (Texto_Inicial == NombreSerie_SinIdentificar))
                        || ( (Texto_Inicial.Length > 1) && (!char.IsLetterOrDigit(SeriesName[0])) ) )
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Li);

                        writer.AddAttribute(HtmlTextWriterAttribute.Href, href_SeriesName);
                        writer.RenderBeginTag(HtmlTextWriterTag.A); // <a href="eLink">
                        writer.Write(SeriesName);
                        writer.RenderEndTag(); // </a>

                        writer.RenderEndTag(); // </li>
                    }
                }
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
                    eLink_decoded = eLink_decoded.Replace("_", " ");
                    string NombreSerie = GetNombreSerie(eLink_decoded);

                    // Si no se dectecto el nombre buscando un patrón (Serie+Episodio), se busca el texto que se va repitiendo:
                    if ( (String.IsNullOrWhiteSpace(NombreSerie)) && (i < TxT_Contenido.Length - 1) )
                    {
                        //string eLink_decoded_siguiente = HttpUtility.UrlDecode(TxT_Contenido[i + 1]);
                        //NombreSerie = GetNombreSerieSiSeRepite(eLink_decoded, eLink_decoded_siguiente, LongitudTextoComunMinima);

                        float Porcentaje_variacion_maximo = 0.8F;
                        LongitudTextoComunMinima = 6; // Nº caracteres comunes con el siguiente para considerarse de la misma serie
                        NombreSerie = GetNombreSerieSiSeRepite(TxT_Contenido, i, Porcentaje_variacion_maximo, LongitudTextoComunMinima);
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
            input = EliminaTextoEntreCorchetesParentesis(input).Replace(".", " ").Trim();


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
            string Pattern10 = "(CD|Cd|cd|CD |Cd |cd )[0-9]{1,3}";

            Match match = Regex.Match(input, Pattern, RegexOptions.CultureInvariant);

            if (match.Success)
                NombreSerie = Formate_Nombre_Series(input, match.Index);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern2);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input,Pattern3);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern4);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern5);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern6);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern7);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern8);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern9);

            if (String.IsNullOrWhiteSpace(NombreSerie))
                NombreSerie = CheckPatterIsMatch(input, Pattern10);

            // EliminaGuionFinal (en caso de existir)
            NombreSerie = EliminaGuionFinal(NombreSerie);

            return NombreSerie;
        }

        private static string CheckPatterIsMatch(string input, string Pattern)
        {
            string NombreSerie = string.Empty;

            // Probamos usando el patrón: S<Numero>E<Numero>
            Match match = Regex.Match(input, Pattern, RegexOptions.CultureInvariant);
            if (match.Success)
                NombreSerie = Formate_Nombre_Series(input, match.Index);

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
            eLink_decoded = eLink_decoded.Replace(Protocolo, string.Empty).Replace("_", " ").Trim();

            char Separador = '|';
            int Pos_1er_separador = eLink_decoded.IndexOf(Separador);
            if (Pos_1er_separador > 0)
                eLink_decoded = eLink_decoded.Substring(0, Pos_1er_separador);

            return eLink_decoded.Trim();
        }

        private static string GetNombreSerieSiSeRepite(string input_actual, string input_siguiente, int LongitudTextoComunMinima)
        {
            string NombreSerie = string.Empty;

            input_actual = Format_Decoded_eLink(input_actual).Replace(".", " ").Trim();
            input_siguiente = Format_Decoded_eLink(input_siguiente).Replace(".", " ").Trim();

            // Eliminamos el texto entre parentesis o corchetes:
            input_actual = EliminaTextoEntreCorchetesParentesis(input_actual);
            input_siguiente = EliminaTextoEntreCorchetesParentesis(input_siguiente);

            // EliminaGuionFinal (en caso de existir)
            input_actual = EliminaGuionFinal(input_actual);
            input_siguiente = EliminaGuionFinal(input_siguiente);

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
                // EliminaGuionFinal (en caso de existir)
                NombreSerie = EliminaGuionFinal(NombreSerie).Trim();
            }
            
            return NombreSerie;
        }

        private static string GetNombreSerieSiSeRepite(string[] TxT_Contenido, int index_inicial, float Porcentaje_variacion_maximo, int Umbral_LongitudTextoComunMinima)
        {
            string NombreSerie = string.Empty;

            string input_inicial = Format_Decoded_eLink(HttpUtility.UrlDecode(TxT_Contenido[index_inicial]));
            // Eliminamos el texto entre parentesis o corchetes:
            input_inicial = EliminaTextoEntreCorchetesParentesis(input_inicial).Replace(".", " ").Trim();
            // EliminaGuionFinal (en caso de existir)
            input_inicial = EliminaGuionFinal(input_inicial);

            
            int Longitud_TextoInicial_en_comun_maxima = int.MaxValue;

            for (int i = index_inicial + 1; i < TxT_Contenido.Length; i++)
            {
                string Nombre_serie_actual = Format_Decoded_eLink(HttpUtility.UrlDecode(TxT_Contenido[i])).Trim();
                // Eliminamos el texto entre parentesis o corchetes:
                Nombre_serie_actual = EliminaTextoEntreCorchetesParentesis(Nombre_serie_actual);
                // EliminaGuionFinal (en caso de existir)
                Nombre_serie_actual = EliminaGuionFinal(Nombre_serie_actual).Replace(".", " ").Trim();

                int Longitud_TextoInicial_en_comun = 0;

                while ((Longitud_TextoInicial_en_comun < input_inicial.Length)
                    && (Longitud_TextoInicial_en_comun < Nombre_serie_actual.Length)
                    && (input_inicial.ToLowerInvariant()[Longitud_TextoInicial_en_comun] == Nombre_serie_actual.ToLowerInvariant()[Longitud_TextoInicial_en_comun]))
                {
                    Longitud_TextoInicial_en_comun++;
                }

                if ((Longitud_TextoInicial_en_comun >= Umbral_LongitudTextoComunMinima)
                    && (Longitud_TextoInicial_en_comun < Longitud_TextoInicial_en_comun_maxima))
                {
                    Longitud_TextoInicial_en_comun_maxima = Longitud_TextoInicial_en_comun;
                }

                if (Longitud_TextoInicial_en_comun < Umbral_LongitudTextoComunMinima)
                    break;
            }


            if ((Longitud_TextoInicial_en_comun_maxima <= input_inicial.Length) &&
                (Longitud_TextoInicial_en_comun_maxima >= Umbral_LongitudTextoComunMinima))
            {
                NombreSerie = input_inicial.Substring(0, Longitud_TextoInicial_en_comun_maxima);
                // EliminaGuionFinal (en caso de existir)
                NombreSerie = EliminaGuionFinal(NombreSerie).Trim();
            }

            return NombreSerie;
        }

        private static string EliminaTextoEntreCorchetesParentesis(string input)
        {
            // Eliminamos el texto entre parentesis o corchetes:
            string regex = "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))";
            return Regex.Replace(input, regex, string.Empty).Trim();
        }

        private static string EliminaGuionFinal(string input)
        {
            int index_last_guion = input.LastIndexOf('-');
            // > 80% longitud del nombre
            if (index_last_guion >= (int)(input.Length * 0.8))
            {
                input = input.Remove(index_last_guion).Trim();
            }
            return input.Trim();
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
        

        #region Descomprimir_Assets

        private void Descomprimir_Assets(string DirHTML)
        {
            string Path_ZipFile = Path.Combine(DirHTML, "assets.zip");
            string Path_Dir_Assets = Path.Combine(DirHTML, "assets");

            // 1) Extraer Reource desde el ejecutable:
            File.WriteAllBytes(Path_ZipFile, Resources.assets);

            if (File.Exists(Path_ZipFile))
            {
                // 2) Crear directorio:
                //Directory.CreateDirectory(Path_Dir_Assets);

                //if (Directory.Exists(Path_Dir_Assets))
                //{
                    // 3) Decomprimirlo:
                using (ZipStorer zip = ZipStorer.Open(Path_ZipFile, FileAccess.Read))
                {
                    List<ZipStorer.ZipFileEntry> zip_store = zip.ReadCentralDir();
                    foreach (ZipStorer.ZipFileEntry entry in zip_store)
                    {
                        bool Extracted_OK = zip.ExtractFile(entry, Path.Combine(DirHTML, entry.FilenameInZip));
                    }
                }
                
                // 4) Borrar el zip:
                File.Delete(Path_ZipFile);
            }
        }

        #endregion
        
    }
}
