//    KMLEditor - A small KML files editor to edit the skins for the emulators like Emu48.
//    Copyright (C) 2021 Regis COSNIER
//
//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace KMLEditor
{
    class KMLFileManager
    {
        List<KMLFile> kmlFiles = new List<KMLFile>();
        Dictionary<string, KMLFile> kmlFilesPerFilename = new Dictionary<string, KMLFile>();
        bool isRootKMLFile = true;
        KMLFile kmlFileRoot;

        List<KMLElement> kmlElements = new List<KMLElement>();
        List<KMLElementWithOffset> kmlElementsWithOffset = new List<KMLElementWithOffset>();
        Dictionary<int, KMLElement> kmlElementsPerId = new Dictionary<int, KMLElement>();

        Regex regexKMLBitmap = new Regex(@"^(?<prefix>\s*Bitmap\s*)""(?<filename>.*)""(?<suffix>.*)$");
        Regex regexKMLInclude = new Regex(@"^(?<prefix>\s*Include\s*)""(?<filename>.*)""(?<suffix>.*)$");
        Regex regexKMLSize = new Regex(@"^(?<prefix>\s*Size\s*)(?<number0>\d+)\s*(?<number1>\d+)(?<suffix>.*)$");
        Regex regexKMLOffset = new Regex(@"^(?<prefix>\s*Offset\s*)(?<number0>\d+)\s*(?<number1>\d+)(?<suffix>.*)$");
        Regex regexKMLBackground = new Regex(@"^(?<prefix>\s*Background\s*)(?<suffix>.*)$");
        Regex regexKMLLCD = new Regex(@"^(?<prefix>\s*Lcd\s*)(?<suffix>.*)$");
        Regex regexKMLDigit = new Regex(@"^(?<prefix>\s*Digit\s*)(?<suffix>.*)$");
        Regex regexKMLAnnunciator = new Regex(@"^(?<prefix>\s*Annunciator\s*)(?<number>\d+)(?<suffix>.*)$");
        Regex regexKMLButton = new Regex(@"^(?<prefix>\s*Button\s*)(?<number>\d+)(?<suffix>.*)$");
        Regex regexKMLButtonType = new Regex(@"^(?<prefix>\s*Type\s*)(?<number>\d+)(?<suffix>.*)$");
        Regex regexKMLButtonDown = new Regex(@"^(?<prefix>\s*Down\s*)(?<number0>\d+)\s*(?<number1>\d+)(?<suffix>.*)$");
        Regex regexKMLButtonPress = new Regex(@"^(?<prefix>\s*Press\s*)(?<number>\d+)(?<suffix>.*)$");
        Regex regexKMLButtonRelease = new Regex(@"^(?<prefix>\s*Release\s*)(?<number>\d+)(?<suffix>.*)$");

        public KMLFileManager()
        {
        }

        public void Cleanup()
        {
            kmlFiles.Clear();
            kmlFilesPerFilename.Clear();
            isRootKMLFile = true;
            kmlElements.Clear();
            kmlElementsWithOffset.Clear();
            kmlElementsPerId.Clear();
            BitmapFilename = null;
            RootBasePath = "";
        }

        public ICollection<KMLElement> GetElements() { return kmlElements; }

        public ICollection<KMLElementWithOffset> GetElementsWithOffsetAndSize()
        {
            return kmlElementsWithOffset;
        }

        public void AddKMLElement(KMLElement kmlElement)
        {
            kmlElementsPerId[kmlElement.id] = kmlElement;
            kmlElements.Add(kmlElement);
            if (kmlElement is KMLElementWithOffset)
                kmlElementsWithOffset.Add((KMLElementWithOffset)kmlElement);
        }

        public KMLElement GetElementById(int id)
        {
            return kmlElementsPerId[id];
        }

        public KMLElementWithOffset GetElementWithOffsetById(int id)
        {
            return kmlElementsPerId[id] as KMLElementWithOffset;
        }


        public KMLFile GetRootFile() { return kmlFileRoot; }

        public string BitmapFilename { get; private set; } = null;

        public string RootBasePath { get; private set; } = "";

        public IList<KMLFile> GetFiles() { return kmlFiles; }

        public bool AddKMLFile(String filename)
        {
            KMLFile kmlFile = new KMLFile(isRootKMLFile);
            if (isRootKMLFile)
            {
                RootBasePath = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar;
                kmlFileRoot = kmlFile;
            }

            isRootKMLFile = false;
            if (kmlFile.ReadFile(filename))
            {
                if (AnalyseKMLFile(kmlFile))
                {
                    kmlFiles.Insert(0, kmlFile);
                    return true;
                }
            }
            return false;
        }

        public bool HitTest(Point location, out KMLElementWithOffset resultElement, bool excludeBackground)
        {
            for (int i = kmlElementsWithOffset.Count - 1; i >= 0; i--)
            {
                KMLElementWithOffset element = kmlElementsWithOffset[i];
                if (excludeBackground && element is KMLBackground)
                    continue;
                if (element.HitTest(location))
                {
                    resultElement = element;
                    return true;
                }
            }
            resultElement = null;
            return false;
        }

        public bool AnalyseKMLFile(KMLFile kmlFile)
        {
            if (kmlFile != null)
            {
                bool isInGlobal = false;
                bool isInBackground = false;
                bool isInLCD = false;
                bool isInDigit = false;
                bool isInAnnunciator = false;
                bool isInButton = false;
                bool isInButtonOnDown = false;
                bool isInButtonOnUp = false;
                bool isInScancode = false;

                KMLBackground currentKMLBackground = null;
                KMLLcd currentKMLLcd = null;
                KMLDigit currentKMLDigit = null;
                KMLButton currentButton = null;
                KMLAnnunciator currentKMLAnnunciator = null;

                Match match;

                IList<string> lines = kmlFile.GetLines();
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    string trimmedLine = line.TrimStart();
                    if (trimmedLine.StartsWith("Global"))
                    {
                        isInGlobal = true;
                    }
                    else if (trimmedLine.StartsWith("Background"))
                    {
                        isInBackground = true;
                        match = regexKMLBackground.Match(line);
                        if (match.Success)
                        {
                            currentKMLBackground = new KMLBackground();
                            currentKMLBackground.kmlFile = kmlFile;
                            currentKMLBackground.elementLineNumber = i;
                            AddKMLElement(currentKMLBackground);
                        }
                    }
                    else if (trimmedLine.StartsWith("Lcd"))
                    {
                        isInLCD = true;
                        currentKMLLcd = new KMLLcd();
                        currentKMLLcd.kmlFile = kmlFile;
                        currentKMLLcd.elementLineNumber = i;
                        AddKMLElement(currentKMLLcd);
                    }
                    else if (trimmedLine.StartsWith("Digit"))
                    {
                        isInDigit = true;
                        match = regexKMLDigit.Match(line);
                        if (match.Success)
                        {
                            currentKMLDigit = new KMLDigit();
                            currentKMLDigit.kmlFile = kmlFile;
                            currentKMLDigit.elementLineNumber = i;
                            AddKMLElement(currentKMLDigit);
                        }
                    }
                    else if (trimmedLine.StartsWith("Annunciator"))
                    {
                        isInAnnunciator = true;
                        match = regexKMLAnnunciator.Match(line);
                        if (match.Success)
                        {
                            int number = Utils.ParseInteger(match.Groups["number"].ToString(), -1);
                            currentKMLAnnunciator = new KMLAnnunciator();
                            currentKMLAnnunciator.kmlFile = kmlFile;
                            currentKMLAnnunciator.elementLineNumber = i;
                            currentKMLAnnunciator.Number = number;
                            AddKMLElement(currentKMLAnnunciator);
                        }
                    }
                    else if (trimmedLine.StartsWith("Button"))
                    {
                        isInButton = true;
                        match = regexKMLButton.Match(line);
                        if (match.Success)
                        {
                            int number = Utils.ParseInteger(match.Groups["number"].ToString(), -1);
                            currentButton = new KMLButton();
                            currentButton.kmlFile = kmlFile;
                            currentButton.elementLineNumber = i;
                            currentButton.Number = number;
                            AddKMLElement(currentButton);
                        }
                    }
                    else if (trimmedLine.StartsWith("OnDown"))
                    {
                        isInButtonOnDown = true;
                    }
                    else if (trimmedLine.StartsWith("OnUp"))
                    {
                        isInButtonOnUp = true;
                    }
                    else if (trimmedLine.StartsWith("Scancode"))
                    {
                        isInScancode = true;
                    }
                    else if (trimmedLine.StartsWith("Include"))
                    {
                        match = regexKMLInclude.Match(line);
                        if (match.Success)
                        {
                            string includeFilename = match.Groups["filename"].ToString();
                            AddKMLFile(RootBasePath + includeFilename);
                        }

                    }
                    else if (trimmedLine.StartsWith("End"))
                    {
                        if (isInGlobal)
                        {
                            isInGlobal = false;
                        }
                        else if (isInBackground)
                        {
                            isInBackground = false;
                        }
                        else if (isInLCD)
                        {
                            isInLCD = false;
                        }
                        else if (isInDigit)
                        {
                            isInDigit = false;
                        }
                        else if (isInAnnunciator)
                        {
                            isInAnnunciator = false;
                            currentKMLAnnunciator = null;
                        }
                        else if (isInButton)
                        {
                            if (isInButtonOnDown)
                            {
                                isInButtonOnDown = false;
                            }
                            else if (isInButtonOnUp)
                            {
                                isInButtonOnUp = false;
                            }
                            else
                            {
                                isInButton = false;
                                currentButton = null;
                            }
                        }
                        else if (isInScancode)
                        {
                            isInScancode = false;
                        }
                    }
                    else
                    {
                        if (isInGlobal)
                        {
                            match = regexKMLBitmap.Match(line);
                            if (match.Success)
                            {
                                BitmapFilename = match.Groups["filename"].ToString();
                            }
                        }
                        if (isInBackground)
                        {
                            match = regexKMLSize.Match(line);
                            if (match.Success && currentKMLBackground != null)
                            {
                                currentKMLBackground.SizeWidth = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLBackground.SizeHeight = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLBackground.sizeLineNumber = i;
                            }
                            match = regexKMLOffset.Match(line);
                            if (match.Success && currentKMLBackground != null)
                            {
                                currentKMLBackground.OffsetX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLBackground.OffsetY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLBackground.offsetLineNumber = i;
                            }
                        }
                        if (isInLCD)
                        {
                            match = regexKMLOffset.Match(line);
                            if (match.Success && currentKMLLcd != null)
                            {
                                currentKMLLcd.OffsetX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLLcd.OffsetY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLLcd.offsetLineNumber = i;
                            }
                        }
                        if (isInDigit)
                        {
                            match = regexKMLSize.Match(line);
                            if (match.Success && currentKMLDigit != null)
                            {
                                currentKMLDigit.SizeWidth = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLDigit.SizeHeight = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLDigit.sizeLineNumber = i;
                            }
                            match = regexKMLOffset.Match(line);
                            if (match.Success && currentKMLDigit != null)
                            {
                                currentKMLDigit.OffsetX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLDigit.OffsetY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLDigit.offsetLineNumber = i;
                            }
                        }
                        if (isInAnnunciator)
                        {
                            match = regexKMLSize.Match(line);
                            if (match.Success && currentKMLAnnunciator != null)
                            {
                                currentKMLAnnunciator.SizeWidth = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLAnnunciator.SizeHeight = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLAnnunciator.sizeLineNumber = i;
                            }
                            match = regexKMLOffset.Match(line);
                            if (match.Success && currentKMLAnnunciator != null)
                            {
                                currentKMLAnnunciator.OffsetX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLAnnunciator.OffsetY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLAnnunciator.offsetLineNumber = i;
                            }
                            match = regexKMLButtonDown.Match(line);
                            if (match.Success && currentKMLAnnunciator != null)
                            {
                                currentKMLAnnunciator.DownX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentKMLAnnunciator.DownY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentKMLAnnunciator.downLineNumber = i;
                            }
                        }
                        if (isInButton)
                        {
                            match = regexKMLButtonType.Match(line);
                            if (match.Success && currentButton != null)
                            {
                                currentButton.Type = Utils.ParseInteger(match.Groups["number"].ToString(), -1);
                                currentButton.typeLineNumber = i;
                            }
                            match = regexKMLSize.Match(line);
                            if (match.Success && currentButton != null)
                            {
                                currentButton.SizeWidth = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentButton.SizeHeight = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentButton.sizeLineNumber = i;
                            }
                            match = regexKMLOffset.Match(line);
                            if (match.Success && currentButton != null)
                            {
                                currentButton.OffsetX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentButton.OffsetY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentButton.offsetLineNumber = i;
                            }
                            match = regexKMLButtonDown.Match(line);
                            if (match.Success && currentButton != null)
                            {
                                currentButton.DownX = Utils.ParseInteger(match.Groups["number0"].ToString(), 0);
                                currentButton.DownY = Utils.ParseInteger(match.Groups["number1"].ToString(), 0);
                                currentButton.downLineNumber = i;
                            }

                            if (isInButtonOnDown)
                            {
                            }
                            else if (isInButtonOnUp)
                            {
                            }
                        }
                        if (isInScancode)
                        {
                        }

                    }
                }
                return true;
            }
            return false;
        }

        public void UpdateKMLSource()
        {
            foreach (var kmlElement in GetElements())
            {
                if (kmlElement.isDirty)
                {
                    kmlElement.kmlFile.isDirty = true;

                    IList<string> lines = kmlElement.kmlFile.GetLines();
                    if (kmlElement is KMLElementWithOffsetAndSize)
                    {
                        KMLElementWithOffsetAndSize kmlElementWithOffsetAndSize = (KMLElementWithOffsetAndSize)kmlElement;
                        if (kmlElementWithOffsetAndSize.offsetLineNumber >= 0 && kmlElementWithOffsetAndSize.offsetLineNumber < lines.Count)
                        {
                            Match match = regexKMLOffset.Match(lines[kmlElementWithOffsetAndSize.offsetLineNumber]);
                            if (match.Success)
                                lines[kmlElementWithOffsetAndSize.offsetLineNumber] = string.Format("{0}{1} {2}{3}", match.Groups["prefix"].ToString(), kmlElementWithOffsetAndSize.OffsetX, kmlElementWithOffsetAndSize.OffsetY, match.Groups["suffix"].ToString());
                        }
                        if (kmlElementWithOffsetAndSize.sizeLineNumber >= 0 && kmlElementWithOffsetAndSize.sizeLineNumber < lines.Count)
                        {
                            Match match = regexKMLSize.Match(lines[kmlElementWithOffsetAndSize.sizeLineNumber]);
                            if (match.Success)
                                lines[kmlElementWithOffsetAndSize.sizeLineNumber] = string.Format("{0}{1} {2}{3}", match.Groups["prefix"].ToString(), kmlElementWithOffsetAndSize.SizeWidth, kmlElementWithOffsetAndSize.SizeHeight, match.Groups["suffix"].ToString());
                        }

                        if (kmlElement is KMLButton)
                        {
                            KMLButton kmlButton = (KMLButton)kmlElement;
                            if (kmlButton.elementLineNumber >= 0 && kmlButton.elementLineNumber < lines.Count)
                            {
                                Match match = regexKMLButton.Match(lines[kmlButton.elementLineNumber]);
                                if (match.Success)
                                    lines[kmlButton.elementLineNumber] = string.Format("{0}{1}{2}", match.Groups["prefix"].ToString(), kmlButton.Number, match.Groups["suffix"].ToString());
                            }
                            if (kmlButton.typeLineNumber >= 0 && kmlButton.typeLineNumber < lines.Count)
                            {
                                Match match = regexKMLButtonType.Match(lines[kmlButton.typeLineNumber]);
                                if (match.Success)
                                    lines[kmlButton.typeLineNumber] = string.Format("{0}{1}{2}", match.Groups["prefix"].ToString(), kmlButton.Type, match.Groups["suffix"].ToString());
                            }
                            if (kmlButton.downLineNumber >= 0 && kmlButton.downLineNumber < lines.Count)
                            {
                                Match match = regexKMLSize.Match(lines[kmlButton.downLineNumber]);
                                if (match.Success)
                                    lines[kmlButton.downLineNumber] = string.Format("{0}{1} {2}{3}", match.Groups["prefix"].ToString(), kmlButton.DownX, kmlButton.DownY, match.Groups["suffix"].ToString());
                            }
                        }
                        else if (kmlElement is KMLAnnunciator)
                        {
                            KMLAnnunciator kmlAnnunciator = (KMLAnnunciator)kmlElement;
                            if (kmlAnnunciator.elementLineNumber >= 0 && kmlAnnunciator.elementLineNumber < lines.Count)
                            {
                                Match match = regexKMLAnnunciator.Match(lines[kmlAnnunciator.elementLineNumber]);
                                if (match.Success)
                                    lines[kmlAnnunciator.elementLineNumber] = string.Format("{0}{1}{2}", match.Groups["prefix"].ToString(), kmlAnnunciator.Number, match.Groups["suffix"].ToString());
                            }
                        }
                    }
                    kmlElement.isDirty = false;
                }
            }
        }
    }
}
