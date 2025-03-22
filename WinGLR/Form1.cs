using Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WinGLR
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private string? inputFile = null;

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            var dlgResult = ofdGrammar.ShowDialog(this);
            if (dlgResult == DialogResult.OK)
            {
                inputFile = ofdGrammar.FileName;
                btnGenerate.Enabled = true;
            }
        }

        // The four input/output channels used by the parser

        StreamReader? inputStream = null;
        StreamWriter? outputStream = null;
        StreamWriter? debugStream = null;
        StreamWriter? tableStream = null;

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();
        }

        private static string DropExtension(string p)
        {
            int lastPeriod = p.LastIndexOf('.');
            if (lastPeriod > 0)
                return p[..lastPeriod];
            else
                return p;
        }

        private void Generate()
        {
            try
            {
                // Get the non-extension part of the input file name

                string fileStem = "output";
                if (!string.IsNullOrEmpty(inputFile))
                    fileStem = DropExtension(inputFile);
                if (fileStem.EndsWith(".designer", StringComparison.CurrentCultureIgnoreCase))
                    fileStem = DropExtension(fileStem);

                // Connect the input and output files

                if (inputFile == null)
                    throw new ArgumentException("No input file specified");

                inputStream = new StreamReader(inputFile);
                outputStream = new StreamWriter($"{fileStem}.designer.cs", false);

                // Look to see if a debug output file is required

                if (chkDebugOutput.Checked)
                    debugStream = new StreamWriter($"{fileStem}.debug.txt", false);

                // Deal with output of parser
                // tables in human readable form

                if (chkTables.Checked)
                    tableStream = new StreamWriter($"{fileStem}.tables.txt", false);

                // Build the parser and associated objects

                string errResult;
                if (chkFSM.Checked)
                    errResult = FSMFactory.CreateOfflineStateMachine
                        (inputStream, outputStream, chkErrorToken.Checked, out _);
                else
                {
                    errResult = ParserFactory.CreateOfflineParser
                    (
                        inputStream, outputStream, tableStream,
                        debugStream, chkCompressSates.Checked, chkErrorToken.Checked,
                        chkGLRParser.Checked, out List<string> extRefs
                    );

                    // ParseLR is only used to create offline parsers for
                    // later compilation as part of another project. As a
                    // result, its grammars should not contain assembly
                    // references in the options section.

                    if (extRefs != null && extRefs.Count > 0)
                        errResult += "Offline grammar contains assembly references in the options section.\r\n";
                }

                if (!string.IsNullOrWhiteSpace(errResult))
                {
                    MessageBox.Show(this, errResult, "Creating parser",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                // Ensure all input and output files are closed,
                // whether there was an exception thrown or not

                outputStream?.Close();
                inputStream?.Close();
                debugStream?.Close();
                tableStream?.Close();
            }
        }
    }
}
