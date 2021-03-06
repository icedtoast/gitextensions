﻿using System;
using GitCommands;

namespace GitUI
{
    public sealed partial class FormGoToCommit : GitExtensionsForm
    {
        public FormGoToCommit()
        {
            InitializeComponent();
            Translate();
        }

        public string GetRevision()
        {
            return GitModule.Current.RevParse(commitExpression.Text);
        }


        private void goButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
