﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace beastie {
    public class IUCNCommonNameIssuesReport {
        TaxonNode topNode;
        TextWriter output;

        public IUCNCommonNameIssuesReport(TaxonNode topNode, TextWriter output) {
            this.topNode = topNode;
            this.output = output;
        }

        public void MakeReport() {
            output.WriteLine("https://en.wikipedia.org/wiki/User:Beastie_Bot/IUCN_common_name_issues");
            output.WriteLine("A list of possible common name errors or issues of names found in the IUCN Red List (" + FileConfig.Instance().iucnRedListFileShortDate  + "). IUCN data downloaded " + FileConfig.Instance().iucnRedListFileDate);
            output.WriteLine("Include actual errors, possible errors, and some special database entry formatting choices that third parties using the data may need to be aware of.");
            KnownSpelling();
            KnownPlurals();
            WeirdJoiners();
            Dot();
            DoubleSpace();
            OddApostrophe();
            QuestionMark();
            Symbols();
            Numbers();
            The();
            FB();
            SpeciesCode();
            AllCaps();
            OddCaps();
            PossiblePlurals();
            SpNov();
            SymbolsInScientificName();
            SciNameTypos(); // Typos found in scientific names
            Stats();
        }


        public void WeirdJoiners() {
            output.WriteLine("==Separators==");
            output.WriteLine("Entries which appear to break the convention of using a comma to separate common names (or have odd formatting along those lines).");
            bool issueFound = false;
            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField != null && namesField.Contains(" - ")) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + namesField + "), dash with spaces (remove whitespace or replace with a comma)");
                    issueFound = true;
                } else if (namesField != null && namesField.Contains("--")) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + namesField + "), double dash, '--'. Perhaps should be a comma (,)");
                    issueFound = true;
                }
            }

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField != null && namesField.Contains(";")) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + namesField + "), contains semicolon (;). Perhaps should be a comma (,)");
                    issueFound = true;
                }
            }

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField != null && namesField.Contains(" or ")) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + namesField + "), 'or'. Perhaps should be a comma (,)");
                    issueFound = true;
                }
            }

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField != null && Regex.IsMatch(namesField, @"\(.*\,.*\)")) { // ( ... , ... )
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' \"" + namesField + "\" — name field contains a comma inside parentheses. Trouble for any software which splits common names at commas.");
                    issueFound = true;
                }
            }


            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void The() {
            output.WriteLine("==Redundant ''the''==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Common name begins with 'the' (Probably redundant)");
                        issueFound = true;
                    }
                }

            }
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }


        public void DoubleSpace() {
            output.WriteLine("==Double space==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.Contains("  ")) {
                        string showspaces = name.Replace(" ", "&nbsp;");
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + showspaces + "): Common name contains double space");
                        issueFound = true;
                    }
                }

            }
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void QuestionMark() {
            output.WriteLine("==Question marks==");
            output.WriteLine("Common name contains one or more question marks, generally due to Unicode characters which cannot be encoded to the IUCN's export (a non-Unicode CSV file).");
            output.WriteLine("May indicate the English common name is not English.");
            output.WriteLine("Also note ''Bristly Cave Crayfish'' which contains an odd character which looks like a Latin 'B'.");
            output.WriteLine();
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.Contains("?")) {
                        string extraNote = "";
                        if (name.Contains("?ristly")) {
                            extraNote = @" — uses a [https://en.wiktionary.org/wiki/%CE%92 Greek capital beta character] instead of a regular Latin B in ""Bristly""";
                        }
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + ")" + extraNote);
                        issueFound = true;
                    }
                }

            }
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void KnownSpelling() {
            output.WriteLine("==Found spelling errors==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                //string[] errors = { "africansspeckled", "tropiacl", "sspined", "mout" };
                // if (Regex.IsMatch(name, @"\b(tropiacl|sspined|mout)\b", RegexOptions.IgnoreCase)) {

                foreach (string name in names) {
                    string lower = name.ToLowerInvariant();

                    if (Regex.IsMatch(name, @"\b(mout)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Known spelling error ('mout' should probably be 'mount').");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"\b(tropiacl)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Known spelling error (tropiacl).");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"\b(ss)", RegexOptions.IgnoreCase)) {
                        // e.g. Cobitis puncticulata = brown spined loach // listed as 'Brown Sspined Loach'
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Oddly starts with a double s.");
                        issueFound = true;
                    } else if (lower.Contains((bitri.genus + "eng").ToLowerInvariant()) || lower.Contains((bitri.epithet + "eng").ToLowerInvariant())) {
                        // Sphenomorphus decipiens = black-sided sphenomorphus // listed as 'Black-sided Sphenomorphuseng'
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Scientific name + 'eng' (possible error).");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"\BSs\B")) { // \B = non-word boundry
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ...Ss...");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"(crayfis)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''crayfis'' should be ''crayfish''.");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(eiongat)", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''eiongate'' should be ''elongate''");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"(girlded)\b", RegexOptions.IgnoreCase)) {
                        // Cordylus tasmani, listed as: Tasman's girlded lizard
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''girlded'' should be ''girdled''");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"\b(meiers)\b", RegexOptions.IgnoreCase)) {
                        // Geoscincus haraldmeieri (Meier's Skink / Meiers Skink)
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''Meiers'' should be ''Meier's''");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"\b(Britian)\b", RegexOptions.IgnoreCase)) {
                        // Britian / Britain
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''Britian'' should be ''Britain''");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"(-english)\b", RegexOptions.IgnoreCase)) {
                        //test me
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): '-English' appended to name.");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(Beyshehir)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''Beyshehir'' should be ''Beysehir'' or ''Beyşehir''");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(Bey[ş\?]hehir)\b", RegexOptions.IgnoreCase)) {
                        // Oxynoemacheilus atili, "Lake Beyşhehir Loach"
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''Beyşhehir'' should be ''Beyşehir''");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(chamaeleon)\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(name, @"\b(chameleon)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Perhaps deliberately using the Latin, but in Enligsh it is spelled ''chameleon'' in over 98% of cases.");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(pellonul)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''pellonul'' should be ''pellonuline''.");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"dfgadfg", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): dfgadfg?");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(ocurring)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''ocurring'' should be ''occurring''.");
                        issueFound = true;

                    } else if (Regex.IsMatch(name, @"\b(selenipidum)\b", RegexOptions.IgnoreCase)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): ''Selenipidum'' should be ''Selenipedium''.");
                        issueFound = true;
                    }

                }
            }
            
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void Numbers() {
            output.WriteLine("==Numbers==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.Any(char.IsNumber)) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Common name contains numbers (possible error)");
                        issueFound = true;
                    }
                }

            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void Symbols() {
            output.WriteLine("==Symbols==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    // e.g. Main´s nipple-cactus
                    char softHyphen = '\u00AD';
                    var oddSymbols = (@"!@#$%^&*_+[]/\|~:{}" + softHyphen).ToCharArray(); // semicolon (;) already listed in Separators. weird accent (´) elsehwere too
                    bool match = name.IndexOfAny(oddSymbols) != -1;
                    bool alreadyCovered = name.ToLowerInvariant().Contains("species code");

                    if (match && !alreadyCovered) {
                        if (name.IndexOf(softHyphen) != -1) {
                            output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name.Replace(softHyphen+"", "&shy;") + "): Common name contains soft-hyphen control character");
                        } else {
                            output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Common name contains symbol(s)");
                        }
                        issueFound = true;
                    }
                }

            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void OddApostrophe() {
            output.WriteLine("==Apostrophe==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.IndexOf('´') != -1) {
                        // e.g. Main´s nipple-cactus
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + ") — common name contains acute accent (´), possibly used as apostrophe (')");
                        issueFound = true;
                    } else if (name.IndexOfAny("´’‛ˈ".ToCharArray()) != -1) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + ") — common name contains possible strange apostrophe");
                        issueFound = true;
                    }

                }
            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void Dot() {
            output.WriteLine("==Dot==");
            //output.WriteLine("The punctuation is mostly redundant, but may be due to an abbreviated entry, e.g. \"Sharpnosed sawtooth pellonul.\" (now fixed)");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.EndsWith(".")) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + ") — Common name ends with a dot");
                        issueFound = true;
                    }
                }

            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }




        public void FB() {
            output.WriteLine("==FB==");
            output.WriteLine("Names ending in '(fb)'");
            output.WriteLine();
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());
                foreach (string name in names) {
                    var lower = name.ToLowerInvariant();
                    if (lower.EndsWith("(fb)")) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' — " + name);
                        issueFound = true;
                    }
                }

            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void SpeciesCode() {
            output.WriteLine("==Species code==");
            output.WriteLine("Common names which are actually species codes");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());
                foreach (string name in names) {
                    var lower = name.ToLowerInvariant();
                    if (lower.Contains("species code")) {
                        output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + ")");
                        issueFound = true;
                    }
                }

            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        HashSet<string> knownPluralNames = new HashSet<string>();

        public void KnownPlurals() {
            output.WriteLine("==Likely plurals==");
            bool issueFound = false;

            // ok: texas
            // but: king of the mullets, Crown Of Thorns, Baby's Tears
            // uncertain: drummers gobbleguts jumbos grass-eaters paperbones barreleyes aurochs spiderlegs pepperpants saddlebags Turkey-peas grains dreams

            string[] knownPlurals = "toads frogs crabs bats anchovies cats snails mullets snappers razorback tetras silversides herrings badgers snakes treefrogs fishes wrasses rats carps".Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            knownPlurals = knownPlurals.Distinct().OrderBy(a => a).ToArray();
            output.WriteLine("Names ending with: " + knownPlurals.JoinStrings(", "));
            output.WriteLine();

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());
                foreach (string name in names) {
                    var lower = name.ToLowerInvariant();

                    if (lower.EndsWith("s")) {
                        if (lower.Contains(" of the ")) continue; // (King Of The Mullets, King Of The Breams)
                        if (lower.Contains(" of ") || lower.Contains("-of-")) continue; // (Crown-of-thorns, Crown Of Thorns)
                        if (knownPlurals.Any(pl => lower.EndsWith(pl))) {
                            output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): Common name is a common plural");
                            knownPluralNames.Add(lower);

                            issueFound = true;
                        }
                    }
                }
            }


            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void PossiblePlurals() {
            output.WriteLine("==Possible plurals==");
            bool issueFound = false;

            string[] exceptions = "steenbras galaxias seps ss ops us mys is eros melidectes cinclodes 's andes texas charaxes goviós diopetes".Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            exceptions = exceptions.Distinct().OrderBy(a => a).ToArray();
            // ss: bass, ass, cypress, albatross, grass, moss
            //string exceptions = "sweetlips", "galaxias", "seps"?

            output.WriteLine("Names ending in ''s''. Common names are usually listed as singular, but these ones appear to be plural. ");
            output.WriteLine();
            output.WriteLine("Ignoring names ending with: " + exceptions.JoinStrings(", ") + ". ");
            output.WriteLine("Also ignoring names containing 'of the', such as 'King Of The Breams', and 'de', such as 'Cyprès de l'Atlas'.");
            output.WriteLine();
            output.WriteLine("Most of these are false positives, but some might possibly be plurals which should be singular.");
            output.WriteLine();

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());
                foreach (string name in names) {
                    var lower = name.ToLowerInvariant();

                    if (!lower.EndsWith("s")) continue;
                    if (lower.Contains(" of the ")) continue;
                    if (lower.Contains(" of ")) continue;
                    if (lower.Contains("-of-")) continue;
                    if (lower.Contains(" de ")) continue;
                    if (knownPluralNames.Contains(lower)) continue; // already done in known plurals list
                    if (exceptions.Any(lower.EndsWith)) continue;
                    if (lower.Contains("species code")) continue; // dealt with elsewhere
                    if (lower.EndsWith(bitri.genus.ToLowerInvariant())) continue;
                    if (lower.EndsWith(bitri.epithet)) continue;

                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' (" + name + "): — possible plural");
                    issueFound = true;
                }

            }


            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void AllCaps() {
            output.WriteLine("==All caps==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (name.Any(char.IsLetter) && !name.Any(char.IsLower)) {
                        string correctCase = RedListCapsReport.CorrectCaps(name).UpperCaseFirstChar();
                        correctCase = correctCase.Replace("mediterranean", "Mediterranean"); // hack

                        // check if already has alternative case version
                        string alt = null;
                        foreach (string othername in names) {
                            if (othername == name) continue;
                            if (othername.ToLower() == name.ToLower()) {
                                alt = othername;
                            }
                        }

                        if (alt != null) {
                            output.WriteLine("* " + bitri.NameLinkIUCN() + " (" + name + ") — redundant all caps name. Also has name listed as: " + '"' + alt + '"');
                            issueFound = true;

                        } else {
                            output.WriteLine("* " + bitri.NameLinkIUCN() + " (" + name + ") — all caps name. Suggested: " + correctCase);
                            issueFound = true;

                        }
                    }
                }

            }
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void OddCaps() {
            output.WriteLine("==Odd caps==");
            bool issueFound = false;

            string[] exclusions = "De Mc Mac Van d' l'".Split().Distinct().OrderBy(a => a).ToArray();

            output.WriteLine("Ignoring names starting with: " + exclusions.JoinStrings(", "));
            output.WriteLine();

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                if (namesField == null) continue;

                var names = namesField.Split(new char[] { ',' }).Select(m => m.Trim());

                foreach (string name in names) {
                    if (Regex.IsMatch(name, @"[a-z]\'[A-Z]")) { // lowercase ' Uppercase
                        if (exclusions.Any(ex => Regex.IsMatch(name, @"\b" + ex))) continue; // exlcusion must be found at start of a word (\b)
                        output.WriteLine("* " + bitri.NameLinkIUCN() + " (" + name + ") — odd caps with apostrophe");
                        issueFound = true;
                    } else if (Regex.IsMatch(name, @"[a-z][A-Z]")) {
                        if (exclusions.Any(ex => Regex.IsMatch(name, @"\b" + ex))) continue;
                        output.WriteLine("* " + bitri.NameLinkIUCN() + " (" + name + ") — camel case");
                        issueFound = true;
                    }
                }

            }
            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }


        public void SymbolsInScientificName() {
            output.WriteLine("==Symbols in scientific name==");
            bool issueFound = false;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string name = bitri.FullDebugName();
                if (name == null) continue;
                var okSymbols = @"' .""-".ToCharArray(); // ok symbols: ' space . " - 
                //var oddSymbols = @"!@#$%^&*_+[]/\|~:{};´".ToCharArray();
                //bool match = name.IndexOfAny(oddSymbols) != -1;
                bool match = name.Any(ch => (Char.IsSymbol(ch) || Char.IsPunctuation(ch)) && !okSymbols.Contains(ch));
                if (!match) continue;
                // An ' may be found in, e.g. .. Chiloglanis sp. nov. 'Kerio'
                output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' — contains symbol(s)");
                issueFound = true;
            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void SpNov() {
            output.WriteLine("==Species Nova==");
            bool issueFound = false;

            int doubleQuote = 0;
            int singleQuote = 0;
            int empty = 0;
            int other = 0;

            string singleEg = "";
            string doubleEg = "";
            string emptyEg = "";
            string otherEg = "";

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string name = bitri.FullDebugName();
                if (name == null) continue;
                var match = Regex.Match(name, @"sp(ecies)?[\W]*nov(a)?[\W]", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                if (match.Value != "sp. nov.") {
                    output.Write("* ''" + bitri.NameLinkIUCN() + "'' — contains odd variation of \"sp. nov.\"");
                    issueFound = true;
                }

                var doubleQuotes = Regex.Match(name, @""".*"""); // contains two double quotes
                var singleQuotes = Regex.Match(name, @"'.*'"); // contains two single quotes

                if (doubleQuotes.Success && singleQuotes.Success) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' — double and single quotes?");
                    issueFound = true;
                    other++;
                } else if (!doubleQuotes.Success && !singleQuotes.Success) {
                    if (name.EndsWith(match.Value)) {
                        // e.g. "Amomum sp. nov."
                        //output.WriteLine("* ''" + name + "'' — no sp. nov. name"); // not really an issue
                        emptyEg = name;
                        empty++;

                    } else {
                        // e.g. "Maytenus sp. nov. A"
                        //output.WriteLine("* ''" + name + "'' — no quotes"); 
                        //issueFound = true; // not really an issue
                        otherEg = name;
                        other++;
                    }

                } else if (doubleQuotes.Success) {
                    output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' — uses double quotes (most in the IUCN database use single quotes)"); // is it an issue?

                    doubleEg = name;
                    doubleQuote++;
                    issueFound = true;

                } else if (singleQuotes.Success) {
                    singleEg = name;
                    singleQuote++;
                }

            }

            output.WriteLine();
            output.WriteLine("Counts:");
            output.WriteLine("* Single quote: " + singleQuote + (singleEg == string.Empty ? "" : " — e.g. ''" + singleEg + "''"));
            output.WriteLine("* Double quote: " + doubleQuote + (doubleEg == string.Empty ? "" : " — e.g. ''" + doubleEg + "''"));
            output.WriteLine("* Empty: " + empty + (emptyEg == string.Empty ? "" : " — e.g. ''" + emptyEg + "''"));
            output.WriteLine("* Other: " + other + (otherEg == string.Empty ? "" : " — e.g. ''" + otherEg + "''"));

            if (!issueFound && (doubleQuote == 0 || singleQuote == 0)) {
                output.WriteLine();
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }

        public void Stats() {
            int sp = 0;
            int ssp = 0;
            int spWithName = 0;
            int sspWithName = 0;
            int spNames = 0;
            int sspNames = 0;

            foreach (var bitri in topNode.DeepBitris().Where(bt => !bt.isStockpop)) {
                string namesField = bitri.CommonNameEng;
                int namesCount = 0;
                if (!string.IsNullOrWhiteSpace(namesField)) {
                    namesCount = 1 + namesField.Count(ch => ch == ',');
                    //namesField.Split(new char[] { ',' }).Select(m => m.Trim());
                }

                if (bitri.isTrinomial) {
                    ssp++;
                    if (namesCount > 0) sspWithName++;
                    sspNames += namesCount;

                } else {
                    sp++;
                    if (namesCount > 0) spWithName++;
                    spNames += namesCount;
                }
            }

            output.WriteLine("==Stats==");
            output.WriteLine("* {0} of {1} species have at least one English common name. ({2}) ", spWithName, sp, TaxonHeaderBlurb.Percent(spWithName, sp));
            output.WriteLine("* {0} of {1} subspecies (infraspecies) have at least one English common name. ({2})", sspWithName, ssp, TaxonHeaderBlurb.Percent(sspWithName, ssp));
            output.WriteLine("* {0} of {1} taxa (species+subspecies) have at least one English common name. ({2})", (spWithName + sspWithName), sp + ssp, TaxonHeaderBlurb.Percent(spWithName + sspWithName, sp + ssp));
            output.WriteLine("* {0} total species common names", spNames);
            output.WriteLine("* {0} total subspecies common names", sspNames);
            output.WriteLine("* {0} total common names", spNames + sspNames);
            //output.WriteLine("* {0} per species, or {1} per species with at least one", spNames);

            output.WriteLine();
        }

        public void SciNameTypos() {
            var typos = TaxaRuleList.Instance().ScientificTypos;
            bool issueFound = false;
            output.WriteLine("==Typos found in binomial names==");
            output.WriteLine("Please consider these changes carefully. If the name isn't changed, at least check that the alternative is listed in the synonyms.");
            output.WriteLine();

            foreach (var bitri in topNode.DeepBitris().Where(bt => typos.ContainsKey(bt.BasicName()))) {
                output.WriteLine("* ''" + bitri.NameLinkIUCN() + "'' should be ''" + typos[bitri.BasicName()] + "''");
                issueFound = true;
            }

            if (!issueFound) {
                output.WriteLine("No issues found.");
            }
            output.WriteLine();
        }
    }
}