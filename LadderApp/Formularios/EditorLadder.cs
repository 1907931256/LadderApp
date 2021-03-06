using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Threading;
using LadderApp.Formularios;
using System.Security;
using LadderApp.Exce��es;
using LadderApp.CodigoInterpretavel;
using System.Diagnostics;

namespace LadderApp
{
    public partial class EditorLadder : Form
    {
        private Thread newThread;// = new Thread(new ParameterizedThreadStart(ProgramaBasico.ExecutaSimuladoTemporizadores));

        private ProjetoLadder frmProj = null;


        public delegate void InvalidateDiagrama();
        public InvalidateDiagrama myDelegateInvalidateDiagrama;


        public delegate void UncheckBtnSimularType();
        public UncheckBtnSimularType myDelegateUncheckBtnSimular;
        
        public EditorLadder()
        {
            this.AutoScroll = false;
            this.VScroll = false;
            this.HScroll = false;
            InitializeComponent();

            myDelegateInvalidateDiagrama = new InvalidateDiagrama(InvalidateDiagramaMethod);

            myDelegateUncheckBtnSimular = new UncheckBtnSimularType(UncheckBtnSimularMethod);

            InitializePrintPreviewDialog();
        }

        private void ShowNewForm(object sender, EventArgs e)
        {
            // Create a new instance of the child form.
            if (this.MdiChildren.Length == 0)
            {
                frmProj = new ProjetoLadder();
                frmProj.MdiParent = this;
                frmProj.Show();
                frmProj.Text = "Sem Nome";
            }
            else
            {
                DialogResult _result = MessageBox.Show(RecursoVisual.STR_QUESTIONA_SALVAR_PROJETO.Replace("%%", frmProj.Text.Trim()).Trim(), "EditorLadder",MessageBoxButtons.YesNo,MessageBoxIcon.Question);

                if (_result == DialogResult.Yes)
                {
                    frmProj.Close();
                }
            }
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            try
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }
            catch (SecurityException ex)
            {
                MessageBox.Show("Erro na seguran�a. Imposs�vel continuar." + ex.Message + " " + ex.PermissionState);
                return;
            }
            openFileDialog.Filter = "Arquivos Ladder (*.xml;*.a43)|*.xml;*.a43|Arquivos XML (*.xml)|*.xml|Execut�vel MSP430 (*.a43)|*.a43";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = "";
                FileName = openFileDialog.FileName;
                // TODO: Add code here to open the file.

                switch (FileName.Substring(FileName.Length - 4, 4).ToLower())
                {
                    case ".xml":

                        try
                        {
                           // Using a FileStream, create an XmlTextReader.
                            Stream fs = new FileStream(FileName, FileMode.Open);
                            XmlReader reader = new XmlTextReader(fs);
                            XmlSerializer serializer = new XmlSerializer(typeof(ProgramaBasico));
                            if (serializer.CanDeserialize(reader))
                            {
                                Object o = serializer.Deserialize(reader);

                                ((ProgramaBasico)o).StsPrograma = ProgramaBasico.StatusPrograma.ABERTO;

                                frmProj = new ProjetoLadder((ProgramaBasico)o);
                                frmProj.programa.PathFile = FileName;
                                frmProj.MdiParent = this;
                                frmProj.Show();
                                frmProj.SetText();
                            }
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erro ao ler o arquivo! " + ex.InnerException.Message, "Abrir arquivos ...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        break;
                    case ".a43":
                        try
                        {
                            ModuloIntegracaoMSP430 p = new ModuloIntegracaoMSP430();

                            String strLido = p.ConvertHex2String(FileName);

                            if (VerificaSenha(strLido))
                                LerExecutavel(strLido, FileName.Substring(FileName.LastIndexOf(@"\") + 1, FileName.Length - FileName.LastIndexOf(@"\") - 1));
                        }
                        catch
                        {
                            MessageBox.Show("Formato desconhecido!", "Abrir arquivos ...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        break;
                    default:
                        MessageBox.Show("Formato desconhecido!", "Abrir arquivos ...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }

            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsProjetoAberto())
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Arquivos XML (*.xml)|*.xml";
            saveFileDialog.FileName = frmProj.programa.Nome + ".xml";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                Salvar(saveFileDialog.FileName);
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Use System.Windows.Forms.Clipboard to insert the selected text or images into the clipboard
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Use System.Windows.Forms.Clipboard to insert the selected text or images into the clipboard
            List<SimboloBasico> _lstSB = null;
            //SimboloBasico _sB = null;
            if (IsDiagramaAberto())
            {
                if (frmProj.frmDiagLadder.ControleSelecionado != null)
                    if (!frmProj.frmDiagLadder.ControleSelecionado.IsDisposed)
                    {
                        CodigosInterpretaveis _cI = frmProj.frmDiagLadder.ControleSelecionado.getCI();
                        switch (_cI)
                        {
                            case CodigosInterpretaveis.PARALELO_INICIAL:
                            case CodigosInterpretaveis.PARALELO_PROXIMO:
                            case CodigosInterpretaveis.PARALELO_FINAL:
                                _lstSB = frmProj.frmDiagLadder.VariosSelecionados(frmProj.frmDiagLadder.ControleSelecionado, frmProj.frmDiagLadder.LinhaSelecionada);
                                break;
                            default:
                                _lstSB = frmProj.frmDiagLadder.VariosSelecionados(frmProj.frmDiagLadder.ControleSelecionado, frmProj.frmDiagLadder.LinhaSelecionada);
                                break;
                        }

                        DataFormats.Format myFormat = DataFormats.GetFormat("List<SimboloBasico>");

                        try
                        {
                            // Insert code to set properties and fields of the object.
                            XmlSerializer mySerializer = new XmlSerializer(typeof(List<SimboloBasico>));
                            XmlSerializer mySerializer2 = new XmlSerializer(typeof(DispositivoLadder));
                            // To write to a file, create a StreamWriter object.
                            StreamWriter myWriter = new StreamWriter("myFileName.xml");
                            StreamWriter myWriter2 = new StreamWriter("myDevice.xml");
                            mySerializer.Serialize(myWriter, _lstSB);
                            mySerializer2.Serialize(myWriter2, frmProj.programa.dispositivo);
                            myWriter.Close();
                            myWriter2.Close();

                            Clipboard.SetData(myFormat.Name, _lstSB);
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show(ex.InnerException.Message);
                        }

                    }
            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Use System.Windows.Forms.Clipboard.GetText() or System.Windows.Forms.GetData to retrieve information from the clipboard.
            if (IsDiagramaAberto())
            {
                if (frmProj.frmDiagLadder.ControleSelecionado != null)
                    if (!frmProj.frmDiagLadder.ControleSelecionado.IsDisposed)
                    {
                        DataFormats.Format myFormat = DataFormats.GetFormat("List<SimboloBasico>");
                        Object returnObject = null;
                        List<SimboloBasico> _lstSB = new List<SimboloBasico>();
                        ListaSimbolo _lstSB2 = new ListaSimbolo();

                        IDataObject iData = Clipboard.GetDataObject();

                        // Determines whether the data is in a format you can use.
                        if (iData.GetDataPresent(myFormat.Name))
                        {
                            try
                            {
                                returnObject = iData.GetData(myFormat.Name);
                            }
                            catch
                            {
                                MessageBox.Show("Erro");
                            }
                        }

                        _lstSB = (List<SimboloBasico>)returnObject;

                        _lstSB2.InsertAllWithClearBefore(_lstSB);

                        ControleLivre _controle = frmProj.frmDiagLadder.LinhaSelecionada.InsereSimboloIndefinido(true, frmProj.frmDiagLadder.ControleSelecionado, _lstSB2);
                        frmProj.frmDiagLadder.ReorganizandoLinhas();
                        _controle.Select();
                    }
            }
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        public void ArrangeProjeto()
        {
            if(this.MdiChildren.Length > 0)
            {
                if (IsProjetoAberto())
                {
                    if(!frmProj.ValidaDiagrama())
                        frmProj.AbreDiagramaLadder();
                }

                this.LayoutMdi(MdiLayout.TileVertical);
                int _quartoHorizontal = frmProj.Width / 2;

                frmProj.Width = _quartoHorizontal - 1;
                frmProj.Location = new Point(0, 0);
                frmProj.frmDiagLadder.Width = 3 * (_quartoHorizontal - 1);
                frmProj.frmDiagLadder.Location = new Point(frmProj.Width, frmProj.frmDiagLadder.Location.Y);
                frmProj.frmDiagLadder.Activate();

                frmProj.frmDiagLadder.ReorganizandoLinhas();
            }
        }

        private void ArrangeProjetoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrangeProjeto();
        }

        private void InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo _lIS, params CodigosInterpretaveis[] _cI)
        {
            if (!IsDiagramaAberto())
                return;

            if (frmProj.frmDiagLadder.ControleSelecionado.IsDisposed)
                return;

            /// aborta a simula��o quando for executar uma altera��o
            if (btnSimular.Checked)
            {
                btnSimular.Checked = false;
                Thread.Sleep(100);
            }

            ControleLivre _controle = frmProj.frmDiagLadder.ControleSelecionado;
            LinhaCompletaVisual _linha = frmProj.frmDiagLadder.LinhaSelecionada;

            _controle = _linha.InsereSimbolo(_lIS, _controle, _cI);

            /// Redesenha linhas e fundo
            frmProj.frmDiagLadder.ReorganizandoLinhas();
            //_linha.simboloDesenhoFundo.Invalidate();

            _controle.Select();
        }

        private void btnContatoNA_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SIMBOLOS, CodigosInterpretaveis.CONTATO_NA);
        }

        private void btnContatoNF_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SIMBOLOS, CodigosInterpretaveis.CONTATO_NF);
        }

        private void btnBobinaSaida_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SAIDA, CodigosInterpretaveis.BOBINA_SAIDA);
        }

        private void btnParalelo_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SIMBOLOS, CodigosInterpretaveis.PARALELO_INICIAL, CodigosInterpretaveis.PARALELO_PROXIMO, CodigosInterpretaveis.PARALELO_FINAL);
        }

        private void btnLinha_Click(object sender, EventArgs e)
        {
            if (IsDiagramaAberto())
            {
                frmProj.frmDiagLadder.InsereLinha();
                //frmProj.frmDiagLadder.ReorganizandoLinhas();
            }
        }

        private void btnVerificarLadder_Click(object sender, EventArgs e)
        {
            if (IsDiagramaAberto())
            {
                Boolean _bResult = frmProj.programa.VerificaPrograma();

                if (_bResult)
                    MessageBox.Show("OK");
                else
                    MessageBox.Show("Erro");
            }
        }

        private Boolean IsProjetoAberto()
        {
            if (this.MdiChildren.Length > 0)
            {
                switch (this.MdiChildren.Length)
                {
                    case 0:
                        return false;
                    default:
                        if (this.MdiChildren.Length > 1)
                            return true;
                        return false;
                }
            }
            return false;
        }

        private Boolean IsDiagramaAberto()
        {
            if (this.MdiChildren.Length > 0)
            {
                switch (this.MdiChildren.Length)
                {
                    case 2:
                        return true;
                }
            }
            return false;
        }

        private void btnTemporizador_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SAIDA, CodigosInterpretaveis.TEMPORIZADOR);
        }

        private void btnContador_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SAIDA, CodigosInterpretaveis.CONTADOR);
        }

        private void EditorLadder_FormClosed(object sender, FormClosedEventArgs e)
        {
            /// para garantir que a thread n�o estar� executando qnd a aplica��o fechar
            if (btnSimular.Checked)
            {
                btnSimular.Checked = false;
                Thread.Sleep(200);
            }
            /// fecha a aplica��o
            Application.Exit();
        }


        // Declare a PrintDocument object named document.
        private System.Drawing.Printing.PrintDocument document =
            new System.Drawing.Printing.PrintDocument();

        // Initalize the dialog.
        private void InitializePrintPreviewDialog()
        {

            // Create a new PrintPreviewDialog using constructor.
            this.PrintPreviewDialog1 = new PrintPreviewDialog();

            //Set the size, location, and name.
            this.PrintPreviewDialog1.ClientSize =
                new System.Drawing.Size(400, 300);
            this.PrintPreviewDialog1.Location =
                new System.Drawing.Point(29, 29);
            this.PrintPreviewDialog1.Name = "PrintPreviewDialog1";

            // Associate the event-handling method with the 
            // document's PrintPage event.
            this.document.PrintPage +=
                new System.Drawing.Printing.PrintPageEventHandler
                (document_PrintPage);

            // Set the minimum size the dialog can be resized to.
            this.PrintPreviewDialog1.MinimumSize =
                new System.Drawing.Size(375, 250);

            // Set the UseAntiAlias property to true, which will allow the 
            // operating system to smooth fonts.
            this.PrintPreviewDialog1.UseAntiAlias = true;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {

            if (IsDiagramaAberto())

            // Set the PrintDocument object's name to the selectedNode
            // object's  tag, which in this case contains the 
            // fully-qualified name of the document. This value will 
            // show when the dialog reports progress.
            {
                document.DocumentName = frmProj.frmDiagLadder.Name;
            }

            // Set the PrintPreviewDialog.Document property to
            // the PrintDocument object selected by the user.
            PrintPreviewDialog1.Document = document;

            // Call the ShowDialog method. This will trigger the document's
            //  PrintPage event.
            PrintPreviewDialog1.ShowDialog();
        }

        private void document_PrintPage(object sender,
            System.Drawing.Printing.PrintPageEventArgs e)
        {

            // Insert code to render the page here.
            // This code will be called when the PrintPreviewDialog.Show 
            // method is called.
            //Bitmap bmp = new Bitmap("c:\\img.bmp");

            // Create image.
            Image newImage = Image.FromFile("c:\\img.bmp");
            

            // Create rectangle for displaying image.
            Rectangle destRect = new Rectangle(100, 100, 450, 150);

            // Draw image to screen.
            e.Graphics.DrawImage(newImage, destRect);


            // The following code will render a simple
            // message on the document in the dialog.
            string text = "In document_PrintPage method.";
            System.Drawing.Font printFont =
                new System.Drawing.Font("Arial", 35,
                System.Drawing.FontStyle.Regular);

            e.Graphics.DrawString(text, printFont,
                System.Drawing.Brushes.Black, 0, 0);

        }

        private void btnSimular_Click(object sender, EventArgs e)
        {
            if (btnSimular.Checked == true || simularToolStripMenuItem.Checked == true)
            {
                btnSimular.Checked = true;
                simularToolStripMenuItem.Checked = true;
                newThread = new Thread(new ThreadStart(this.ExecutaSimuladorContinuo));
                newThread.Start();
            }
        }

        /// <summary>
        /// Thread - executa continuamente enquanto a op��o de simula��o estiver ativa
        /// </summary>
        public void ExecutaSimuladorContinuo()
        {
            /// mant�m loop enquanto op��o de simula��o estiver ativa
            while (btnSimular.Checked)
            {

                /// verifica se a janela do diagrama ladder est� aberta
                if (!IsDiagramaAberto())
                {
                    UncheckBtnSimular(false);
                    return;
                }

                /// verifica se o programa ladder n�o est� inconsistente
                if (!frmProj.programa.VerificaPrograma())
                {
                    UncheckBtnSimular(false);
                    return;
                }

                /// executa a fun��o dos temporizadores
                frmProj.programa.ExecutaSimuladoTemporizadores();

                /// executa a l�gica ladder
                if (!frmProj.programa.ExecutaLadderSimulado())
                {
                    UncheckBtnSimular(false);
                    return;
                }

                /// atualiza o janela do diagrama ladder
                this.InvalidaFormulario(true);

                /// aguarda 100 ms
                Thread.Sleep(100);
            }
        }


        private void btnReset_Click(object sender, EventArgs e)
        {
            InsereSimbolo(LinhaCompletaVisual.LocalInsereSimbolo.SAIDA, CodigosInterpretaveis.RESET);
        }

        private void btnSimular_CheckStateChanged(object sender, EventArgs e)
        {
            //btnSimular.BackColor = btnSimular.Checked == true ? Color.Green : Form.DefaultBackColor;
        }

        public void InvalidaFormulario(bool bstate)
        {
            if (frmProj.frmDiagLadder.InvokeRequired)
            {
                this.Invoke(this.myDelegateInvalidateDiagrama);
            }
            else
            {
                this.frmProj.frmDiagLadder.Invalidate(bstate);
            }

        }

        public void UncheckBtnSimular(bool bstate)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(this.myDelegateUncheckBtnSimular);
            }
            else
            {
                btnSimular.Checked = false;
                simularToolStripMenuItem.Checked = false;
            }

        }
        
        public void InvalidateDiagramaMethod()
        {
            frmProj.frmDiagLadder.Invalidate(true);
        }

        public void UncheckBtnSimularMethod()
        {
            btnSimular.Checked = false;
            simularToolStripMenuItem.Checked = false;
        }


        private void LerExecutavel(String DadosConvertidosChar, String strNomeProjeto)
        {
            List<int> lstCodigosLidos = new List<int>();
            CodigosInterpretaveis guarda = CodigosInterpretaveis.NENHUM;

            if (DadosConvertidosChar.IndexOf("@laddermic.com") != -1)
            {
                Int32 intContaFim = 0;
                Int32 intIndiceLinha = 0;
                Int32 iNumOperandos = 0;
                EnderecamentoLadder _endLido;
                TipoEnderecamentoDispositivo _tpEndLido;
                Int32 _iIndiceEndLido = 0;


                /// Cria um programa novo vazio
                ProgramaBasico programa = new ProgramaBasico();
                programa.StsPrograma = ProgramaBasico.StatusPrograma.NOVO;
                programa.Nome = strNomeProjeto;
                programa.dispositivo = new DispositivoLadder(1);
                programa.endereco.AlocaEnderecamentoIO(programa.dispositivo);
                programa.endereco.AlocaEnderecamentoMemoria(programa.dispositivo, programa.endereco.lstMemoria, TipoEnderecamentoDispositivo.DIGITAL_MEMORIA, 10);
                programa.endereco.AlocaEnderecamentoMemoria(programa.dispositivo, programa.endereco.lstTemporizador, TipoEnderecamentoDispositivo.DIGITAL_MEMORIA_TEMPORIZADOR, 10);
                programa.endereco.AlocaEnderecamentoMemoria(programa.dispositivo, programa.endereco.lstContador, TipoEnderecamentoDispositivo.DIGITAL_MEMORIA_CONTADOR, 10);
                intIndiceLinha = programa.InsereLinhaNoFinal(new LinhaCompleta());

                for (int i = DadosConvertidosChar.IndexOf("@laddermic.com") + 15; i < DadosConvertidosChar.Length; i++)
                {
                    guarda = (CodigosInterpretaveis)Convert.ToChar(DadosConvertidosChar.Substring(i, 1));

                    switch (guarda)
                    {
                        case CodigosInterpretaveis.NENHUM:
                            intContaFim++;
                            iNumOperandos = 0;
                            break;
                        case CodigosInterpretaveis.FIM_DA_LINHA:
                            intContaFim++;
                            iNumOperandos = 0;
                            if ((CodigosInterpretaveis)Convert.ToChar(DadosConvertidosChar.Substring(i+1, 1)) != CodigosInterpretaveis.NENHUM)
                            intIndiceLinha = programa.InsereLinhaNoFinal(new LinhaCompleta());
                            break;
                        //case CodigosInterpretaveis.INICIO_DA_LINHA:
                        case CodigosInterpretaveis.CONTATO_NA:
                        case CodigosInterpretaveis.CONTATO_NF:
                            intContaFim = 0;
                            iNumOperandos = 2;
                            {
                                SimboloBasico _sb = new SimboloBasico((CodigosInterpretaveis)guarda);
                                //_sb.setOperando(0, programa.endereco.Find((TipoEnderecamentoDispositivo)Convert.ToChar(DadosConvertidosChar.Substring(i + 1, 1)), (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 2, 1))));

                                _tpEndLido =(TipoEnderecamentoDispositivo)Convert.ToChar(DadosConvertidosChar.Substring(i + 1, 1));
                                _iIndiceEndLido = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 2, 1));
                                _endLido = programa.endereco.Find(_tpEndLido, _iIndiceEndLido);
                                if (_endLido == null)
                                {
                                    programa.dispositivo.lstBitPorta[_iIndiceEndLido - 1].TipoDefinido = _tpEndLido;
                                    programa.dispositivo.RealocaEnderecoDispositivo();
                                    programa.endereco.AlocaEnderecamentoIO(programa.dispositivo);
                                    _endLido = programa.endereco.Find(_tpEndLido, _iIndiceEndLido);
                                }
                                _sb.setOperando(0, _endLido);

                                i += 2;
                                programa.linhas[intIndiceLinha].simbolos.Add(_sb);
                            }
                            break;
                        case CodigosInterpretaveis.BOBINA_SAIDA:
                        case CodigosInterpretaveis.RESET:
                            intContaFim = 0;
                            iNumOperandos = 2;
                            {
                                ListaSimbolo _lstSB = new ListaSimbolo();
                                _lstSB.Add(new SimboloBasico((CodigosInterpretaveis)guarda));
                                _tpEndLido = (TipoEnderecamentoDispositivo)Convert.ToChar(DadosConvertidosChar.Substring(i+1, 1));
                                _iIndiceEndLido = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i+2, 1));
                                _endLido = programa.endereco.Find(_tpEndLido, _iIndiceEndLido);
                                if (_endLido == null)
                                {
                                    programa.dispositivo.lstBitPorta[_iIndiceEndLido - 1].TipoDefinido = _tpEndLido;
                                    programa.dispositivo.RealocaEnderecoDispositivo();
                                    programa.endereco.AlocaEnderecamentoIO(programa.dispositivo);
                                    _endLido = programa.endereco.Find(_tpEndLido, _iIndiceEndLido);
                                }
                                _lstSB[_lstSB.Count - 1].setOperando(0, _endLido);
                                i+=2;
                                programa.linhas[intIndiceLinha].Insere2Saida(_lstSB);
                                _lstSB.Clear();
                            }
                            break;
                        case CodigosInterpretaveis.PARALELO_INICIAL:
                        case CodigosInterpretaveis.PARALELO_FINAL:
                        case CodigosInterpretaveis.PARALELO_PROXIMO:
                            intContaFim = 0;
                            iNumOperandos = 0;
                            programa.linhas[intIndiceLinha].simbolos.Add(new SimboloBasico((CodigosInterpretaveis)guarda));
                            break;
                        case CodigosInterpretaveis.CONTADOR:
                            intContaFim = 0;
                            iNumOperandos = 3;
                            {
                                ListaSimbolo _lstSB = new ListaSimbolo();
                                _lstSB.Add(new SimboloBasico((CodigosInterpretaveis)guarda));
                                _lstSB[_lstSB.Count - 1].setOperando(0, programa.endereco.Find(TipoEnderecamentoDispositivo.DIGITAL_MEMORIA_CONTADOR, (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 1, 1))));
                                ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Contador.Tipo = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 2, 1));
                                ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Contador.Preset = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 3, 1));

                                _lstSB[_lstSB.Count - 1].setOperando(1, ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Contador.Tipo);
                                _lstSB[_lstSB.Count - 1].setOperando(2, ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Contador.Preset);
                                i += 3;
                                programa.linhas[intIndiceLinha].Insere2Saida(_lstSB);
                                _lstSB.Clear();
                            }
                            break;
                        case CodigosInterpretaveis.TEMPORIZADOR:
                            intContaFim = 0;
                            iNumOperandos = 4;
                            {
                                ListaSimbolo _lstSB = new ListaSimbolo();
                                _lstSB.Add(new SimboloBasico((CodigosInterpretaveis)guarda));
                                _lstSB[_lstSB.Count - 1].setOperando(0, programa.endereco.Find(TipoEnderecamentoDispositivo.DIGITAL_MEMORIA_TEMPORIZADOR, (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 1, 1))));
                                ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.Tipo = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 2, 1));
                                ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.BaseTempo = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 3, 1));
                                ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.Preset = (Int32)Convert.ToChar(DadosConvertidosChar.Substring(i + 4, 1));

                                _lstSB[_lstSB.Count - 1].setOperando(1, ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.Tipo);
                                _lstSB[_lstSB.Count - 1].setOperando(2, ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.Preset);
                                _lstSB[_lstSB.Count - 1].setOperando(4, ((EnderecamentoLadder)_lstSB[_lstSB.Count - 1].getOperandos(0)).Temporizador.BaseTempo);
                                
                                i += 4;
                                programa.linhas[intIndiceLinha].Insere2Saida(_lstSB);
                                _lstSB.Clear();
                            }
                            break;
                    }

                    /// fim dos c�digos
                    if (intContaFim >= 2)
                    {
                        /// grava os dados lidos do codigo intepretavel
                        ModuloIntegracaoMSP430 p = new ModuloIntegracaoMSP430();
                        p.CriaArquivo("codigosinterpretaveis.txt", DadosConvertidosChar.Substring(DadosConvertidosChar.IndexOf("@laddermic.com"), i - DadosConvertidosChar.IndexOf("@laddermic.com") + 1));

                        /// for�a sa�da do loop
                        i = DadosConvertidosChar.Length;
                    }
                }
                frmProj = new ProjetoLadder(programa);
                frmProj.MdiParent = this;
                frmProj.Show();
                frmProj.SetText();

            }
            else
                MessageBox.Show("O arquivo n�o foi reconhecido pelo sistema!", "Abrir Arquivos ...", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSobre aboutBox = new frmSobre();
            aboutBox.ShowDialog();
        }

        private void mnuEditarComent�rio_Click(object sender, EventArgs e)
        {
            if (IsDiagramaAberto())
                if (frmProj.frmDiagLadder.ControleSelecionado != null)
                    if (!frmProj.frmDiagLadder.ControleSelecionado.IsDisposed)
                    {
                        SimboloBasico _sb = frmProj.frmDiagLadder.ControleSelecionado.SimboloBasico;
                        if (_sb.getOperandos(0) != null)
                            if ((_sb.getOperandos(0).GetType().Name == "EnderecamentoLadder"))
                            {
                                frmAlteraComentario frmAltComent = new frmAlteraComentario();

                                frmAltComent.txtComentario.Text = ((EnderecamentoLadder)_sb.getOperandos(0)).Apelido.Trim();
                                frmAltComent.Text = frmAltComent.Text.Replace("#ENDERECO#",((EnderecamentoLadder)_sb.getOperandos(0)).Nome);

                                DialogResult _result = frmAltComent.ShowDialog();
                                if (_result == DialogResult.OK)
                                {
                                    ((EnderecamentoLadder)_sb.getOperandos(0)).Apelido = frmAltComent.txtComentario.Text;
                                    frmProj.frmDiagLadder.Invalidate(true);
                                }
                            }
                    }
        }

        private void SalvarArquivo(object sender, EventArgs e)
        {
            if (!IsProjetoAberto())
                return;

            switch (frmProj.programa.StsPrograma)
            {
                case ProgramaBasico.StatusPrograma.ABERTO:
                case ProgramaBasico.StatusPrograma.SALVO:
                    Salvar(frmProj.programa.PathFile);
                    break;
                default:
                    SaveAsToolStripMenuItem_Click(sender, e);
                    break;
            }
        }

        private void Salvar(String FileName)
        {
            try
            {
                // TODO: Add code here to save the current contents of the form to a file.
                XmlSerializer mySerializer = new XmlSerializer(typeof(ProgramaBasico));
                //teste XmlSerializer mySerializer = new XmlSerializer(typeof(DispositivoLadder));
                //XmlSerializer mySerializer = new XmlSerializer(typeof(DispositivoLadder));
                // To write to a file, create a StreamWriter object.
                StreamWriter myWriter = new StreamWriter(FileName);
                mySerializer.Serialize(myWriter, frmProj.programa);
                //teste mySerializer.Serialize(myWriter, frmProj.programa.dispositivo);
                //mySerializer.Serialize(myWriter, frmProj.programa.dispositivo);
                myWriter.Close();
                frmProj.programa.PathFile = FileName;
                frmProj.programa.StsPrograma = ProgramaBasico.StatusPrograma.SALVO;
                frmProj.SetText();
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show("O arquivo n�o pode ser salvo! " + ex.InnerException.Message, "Salvar como...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    MessageBox.Show("O arquivo n�o pode ser salvo! " + ex.Message, "Salvar como...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void op��esToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void mnuGravarLadderNoExecutavel_Click_1(object sender, EventArgs e)
        {
            mnuGravarLadderNoExecutavel.Checked = ((mnuGravarLadderNoExecutavel.Checked == true) ? false : true);
        }

        private void jTAGUSBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jTAGUSBToolStripMenuItem.Checked = true;
            jTAGParaleloToolStripMenuItem.Checked = false;
        }

        private void jTAGParaleloToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jTAGUSBToolStripMenuItem.Checked = false;
            jTAGParaleloToolStripMenuItem.Checked = true;
        }

        private void baixarProgramaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsProjetoAberto())
                return;

            try
            {
                frmProj.programa.GeraExecutavel(mnuGravarLadderNoExecutavel.Checked, mnuSolicitarSenhaParaLerLadder.Checked, true);

                File.Delete(Application.StartupPath + @"\" + frmProj.programa.Nome.Replace(' ', '_') + ".a43");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Escrever Execut�vel");
            }
        }

        private void verificarLadderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsDiagramaAberto())
            {
                Boolean _bResult = frmProj.programa.VerificaPrograma();

                if (_bResult)
                    MessageBox.Show("OK");
                else
                    MessageBox.Show("Erro");
            }
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void mnuGravarLadderNoExecutavel_CheckedChanged(object sender, EventArgs e)
        {
            if (!mnuGravarLadderNoExecutavel.Checked)
            {
                mnuSolicitarSenhaParaLerLadder.Checked = false;
                mnuSolicitarSenhaParaLerLadder.Enabled = false;
            }
            else
                mnuSolicitarSenhaParaLerLadder.Enabled = true;
        }

        private bool bSimulacao = false;
        private void Simulacao(object sender, EventArgs e)
        {
            /// inverte condi��o da simula��o - habilitada / desabilitada
            bSimulacao = (bSimulacao == true ? false : true);

            if (bSimulacao)
            {
                btnSimular.Checked = true;
                simularToolStripMenuItem.Checked = true;
                newThread = new Thread(new ThreadStart(this.ExecutaSimuladorContinuo));
                newThread.Start();
            }
            else
            {
                btnSimular.Checked = false;
                simularToolStripMenuItem.Checked = false;
            }
        }

        private void mnuGerarExecut�vel_Click(object sender, EventArgs e)
        {
            if (!IsProjetoAberto())
                return;

            try
            {
                frmProj.programa.GeraExecutavel(mnuGravarLadderNoExecutavel.Checked, mnuSolicitarSenhaParaLerLadder.Checked, false);
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\" + frmProj.programa.Nome.Replace(' ', '_') + ".a43"))
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\" + frmProj.programa.Nome.Replace(' ', '_') + ".a43");

                File.Move(Application.StartupPath + @"\" + frmProj.programa.Nome.Replace(' ', '_') + ".a43", Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\" + frmProj.programa.Nome.Replace(' ', '_') + ".a43");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Gerar Execut�vel");
            }
        }

        private void lerProgramaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModuloIntegracaoMSP430 p = new ModuloIntegracaoMSP430();
            try
            {
                String strLido = p.LeViaUSB();

                if (VerificaSenha(strLido))
                    LerExecutavel(strLido, "Sem Nome");
            }
            catch (CouldNotInitializeTIUSBException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool VerificaSenha(String strLido)
        {
            Txt2CodigosInterpretaveis txt2CI = null;

            txt2CI = new Txt2CodigosInterpretaveis(strLido);

            if (txt2CI.ExisteCabecalho())
            {
                txt2CI.ObtemInformacoesCabecalho();
                if (txt2CI.bSolicitarSenha)
                {
                    DialogResult _result;
                    //String _strSenha = "";
                    bool _bSenhaOK = false;
                    frmSenha _frmSenha = new frmSenha();

                    _frmSenha.Text = "Digite a senha (1/2):";
                    _frmSenha.lblSenhaAtual.Text = "Senha:";

                    for (int i = 0; i < 2; i++)
                    {
                        _result = _frmSenha.ShowDialog();

                        if (_result == DialogResult.Cancel)
                        {
                            MessageBox.Show("Opera��o cancelada!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return false;
                        }
                        else
                        {
                            if (txt2CI.strSenha != _frmSenha.txtSenha.Text)
                            {
                                _frmSenha.txtSenha.Text = "";
                                _frmSenha.Text = "Digite a senha (1/2):";
                                //return;
                            }
                            else
                            {
                                _bSenhaOK = true;
                                i = 5; //sai
                            }
                        }
                    }
                    if (!_bSenhaOK)
                    {
                        MessageBox.Show("Opera��o cancelada!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }
                }
            }

            return true;
        }

        private void indexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("AcroRd32.exe", Application.StartupPath + @"\MANUALSPVMSP430.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SPVMSP430");
            }
        }
    }

}
