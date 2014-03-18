using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicken.UI.OperatingSystem.Launcher.Enumerations
{
    public enum Verbs
    {
        [Display(Name = "open")]
        Open, 

        [Display(Name = "openas")]
        OpenAs,

        [Display(Name = "opennew")]
        OpenNew,

        [Display(Name = "runas")]
        RunAs,

        [Display(Name = "")]
        Default,

        [Display(Name = "edit")]
        Edit,

        [Display(Name = "explore")]
        Explore,

        [Display(Name = "properties")]
        Properties,

        [Display(Name = "copy")]
        Copy,

        [Display(Name = "cut")]
        Cut,

        [Display(Name = "paste")]
        Paste,

        [Display(Name = "pastelink")]
        PasteLink,

        [Display(Name = "delete")]
        Delete,

        [Display(Name = "print")]
        Print,

        [Display(Name = "printto")]
        PrintTo,

        [Display(Name = "find")]
        Find
    }
}
