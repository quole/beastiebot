﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace beastie {
    public class TaxonHeaderBlurb : Blurb //TODO: split into TaxonStatusStats, Blurb classes, and TaxonNodeName
    {

        // e.g. "==[[Mammalia|Mammals]]==" or "==[[Mammalia]] species=="
        // "groupof" may or may not appear in the returned results
        public static string HeadingString(TaxonNode node, int depth, RedStatus status) {
            bool noHeader0 = true; // no header for depth 0 (e.g. "Mammal species")
            bool skipDepth1 = true; // don't return "=Pangolin species=", instead "==Pangolin species=="

            if (noHeader0 && depth == 0) {
                return string.Empty;
            }


            string groupof = null;
            var stats = node.GetStats(status);
            if (stats.species > 0 && stats.subspecies == 0 && stats.subpops_total == 0) {
                // show "species" in heading only if it's not going to be in a subheading
                groupof = "species";
            }

            int headerDepth = depth;

            if (skipDepth1) {
                if (headerDepth >= 1) {
                    headerDepth++;
                }
            }

            string tabs = string.Empty;
            tabs = new string('=', headerDepth);

            //string line = string.Format("{0}[[{1}]] ({2})", tabs, name, rank);
            string heading = node.nodeName.CommonNameGroupTitleLink(true, groupof);

            string line = string.Format("{0}{1}{0}", tabs, heading, tabs);

            return line;
        }

        //TODO: move to other static class?
        public static string GrayText(TaxonNode node) {
            var rules = node.rules;

            if (rules == null)
                return null;

            if (!string.IsNullOrWhiteSpace(rules.comprises)) {
                //return string.Format(@"{{gray|{0}}}", comprises);
                return "{{gray|" + rules.comprises.UpperCaseFirstChar() + "}}"; // {{gray|comprises}}

            }

            if (!string.IsNullOrWhiteSpace(rules.includes)) {
                return "{{gray|Includes " + rules.includes + ".}}"; // {{gray|Includes includes}}
            }

            if (!string.IsNullOrWhiteSpace(rules.means)) {
                return "{{gray|(\"" + rules.means.UpperCaseFirstChar() + "\")}}"; // {{gray|("means")}} // note: string.Format turns {{ into {.
            }


            return null;
        }

        /*
        // replaced with TaxonName.CommonOrTaxoNameLowerPref()
        public string VernacularStringLower() {
            if (notAssigned) {
                return "\"not assigned\""; // lit: "not assigned" (with quotes)
            }

            return taxonPage.CommonOrTaxoNameLowerPref();
        }
        */


        public static string ArticleBlurb(TaxonNode node, RedStatus status) {

            if (status == RedStatus.EXplus)
                return ExPlusBlurb(node, status);

            StringBuilder blurb = new StringBuilder();
            var cr_stats = node.GetStats(status);
            var all_stats = node.GetStats();

            int cr_count = cr_stats.species; //node.DeepSpeciesCount(status);
            int sp_count = all_stats.species; //ode.DeepSpeciesCount();
            blurb.AppendFormat("As of {0}, the [[International Union for Conservation of Nature]] (IUCN) lists {1} {2}{3}",
                FileConfig.Instance().iucnRedListFileDate, // {0} date
                cr_count.NewspaperNumber(), // {1} species count
                (status == RedStatus.Null ? "" : status.WikiLink() + " "), // {2} status with link (optional) e.g. "[[Vulnerable species|vulnerable]]"
                node.nodeName.Adjectivize(true, false, "species") // {3} taxon species group name ("mammalian species" or "species within Mammalia")
                );

            if (status == RedStatus.CR) {
                int pe_count = node.GetStats(RedStatus.PE).species; //node.DeepSpeciesCount(RedStatus.PE);
                int pew_count = node.GetStats(RedStatus.PEW).species; //node.DeepSpeciesCount(RedStatus.PEW);
                int combined_count = pe_count + pew_count;

                if (combined_count > 0) {
                    if (pe_count > 0 && pew_count == 0) {
                        blurb.AppendFormat(", including {0} which are tagged as ''possibly extinct''.",
                            pe_count
                       );
                    } else {
                        blurb.AppendFormat(", including {0} which are tagged as ''possibly extinct'' or ''possibly extinct in the wild''.",
                            combined_count
                       );

                    }

                } else {
                    blurb.Append(", none of which are tagged as ''possibly extinct''.");
                }
                blurb.Append(FileConfig.Instance().iucnRedListFileRef);
                blurb.Append(FileConfig.Instance().iucnPossiblyExtinctFileRef);
                blurb.Append(" ");

            } else {
                blurb.Append(".");
                blurb.Append(FileConfig.Instance().iucnRedListFileRef);
                blurb.Append(" ");
            }

            if (status != RedStatus.Null && cr_count > 0) {
                blurb.AppendFormat("{0} of all evaluated {1} are listed as {2}. ",
                    Percent(cr_count, sp_count),
                    //was: "{1}species", (taxon == "top" ? "" : taxon + " "),
                    node.nodeName.Adjectivize(false, false, "species"),
                    status.Text()
                    );

                // debug info:
                blurb.AppendFormat("<!-- {0} / {1} -->", cr_count, sp_count);
            }

            blurb.AppendLine();

            bool showAlso = cr_count > 0;
            string alsoSubs = AlsoSubsp(node, status, showAlso);

            if (!string.IsNullOrEmpty(alsoSubs)) {
                blurb.AppendLine(alsoSubs.TrimEnd());
                blurb.AppendLine();
            }

            string subpopsLine = Subpops(node, status);
            if (subpopsLine != null) {
                blurb.AppendLine(Subpops(node, status).TrimEnd());
                blurb.AppendLine();
            }

            string threatenedContextBlurb = ThreatenedContextBlurb(node, status);
            if (!string.IsNullOrEmpty(threatenedContextBlurb)) {
                blurb.AppendLine(threatenedContextBlurb);
                blurb.AppendLine();
            }

            string ddinfo = DDInfo(node, status);
            if (!string.IsNullOrEmpty(ddinfo)) {
                blurb.Append(ddinfo.Trim());
                blurb.AppendLine();
                blurb.AppendLine();
            }

            string notes = LastParagraphNotes(node, status);
            blurb.AppendLine(notes);
            blurb.AppendLine();

            //TODO: check if needed
            blurb.Append(@"{{TOC limit|3}}");

            return blurb.ToString();

        }

        private static string ExPlusBlurb(TaxonNode node, RedStatus status) {
            //throw new NotImplementedException();
            string sectionQs = node.GetSections(status).ToNewspaperQuantities(true, false);

            var regex = new Regex(Regex.Escape("extinct in the wild"));
            sectionQs  = regex.Replace(sectionQs , "[[extinct in the wild]]", 1);

            var regex2 = new Regex(Regex.Escape("extinct species"));
            sectionQs = regex2.Replace(sectionQs, "[[Extinction|extinct]]", 1);

            sectionQs += " of " + node.nodeName.CommonNameLink(false, PrettyStyle.JustNames);

            return string.Format("As of {0}, the [[International Union for Conservation of Nature]] (IUCN) lists {1}.{2}{3}\n\n",
                FileConfig.Instance().iucnRedListFileDate, // {0} date
                sectionQs,
                FileConfig.Instance().iucnRedListFileRef,
                FileConfig.Instance().iucnPossiblyExtinctFileRef
            );
        }

        static string AlsoSubsp(TaxonNode node, RedStatus status, bool showAlso) {
            // The IUCN also lists 59 mammalian subspecies as critically endangered. 
            // TODO: work in the word "globally" ?

            var cr_stats = node.GetStats(status);

            //StringBuilder blurb = new StringBuilder();
            var cr_subsp = cr_stats.subspecies;

            if (cr_subsp == 0)
                return string.Empty;

            if (status == RedStatus.Null) {
                //TODO: varieties
                return "The IUCN " + (showAlso ? "also " : "") + "has evaluated " + cr_subsp.NewspaperNumber() + " " + node.nodeName.Adjectivize(false, false, "subspecies", "within") + ". ";
                
            } else {
                if (cr_stats.subspecies_varieties > 0) {
                    var items = new List<Tuple<int, string>>() {
                        { cr_stats.subspecies_actual_subsp, "subspecies" },
                        { cr_stats.subspecies_varieties, "varieties" }
                    };

                    //string thingsTheyList = (cr_stats.subspecies_actual_subsp + " subspecies, " + cr_stats.subspecies_varieties + " varieties").ToNewspaperQualtities();
                    string thingsTheyList = items.ToNewspaperQualtities();
                    return "The IUCN " + (showAlso ? "also " : "") + "lists " + thingsTheyList + " as " + status.Text() + ". ";

                } else {
                    return "The IUCN " + (showAlso ? "also " : "") + "lists " + cr_subsp.NewspaperNumber() + " " + node.nodeName.Adjectivize(false, false, "subspecies", "within") + " as " + status.Text() + ". ";
                }
            }
        }

        static string Subpops(TaxonNode node, RedStatus status) {
            // Of the mammalian subpopulations evaluated, 17 species subpopulations and 1 subspecies subpopulation have been assessed as critically endangered.
            //if (cr_subsp > 0)
            if (status == RedStatus.EXplus)
                return null; // don't mention the small number of local extinctions tracked as subpopulations

            var cr_stats = node.GetStats(status);

            if (cr_stats.subpops_total == 0) {

                // No mammalian subpopulations have been evaluated as critically endangered by the IUCN."
                int all_subpops_total = node.GetStats().subpops_total;
                if (all_subpops_total == 0) {
                    // No mammalian subpopulations have been evaluated by the IUCN."
                    //return "No " + node.nodeName.Adjectivize(false, false, "subpopulations", "of") + " have been evaluated by the IUCN. ";

                    return "No subpopulations of " + node.nodeName.LowerPluralOrTaxon() + " have been evaluated by the IUCN. ";
                } else {
                    //return "No " + node.nodeName.Adjectivize(false, false, "subpopulations", "of") + " have been evaluated as " + status.Text() + " by the IUCN. ";
                    return "No subpopulations of " + node.nodeName.LowerPluralOrTaxon() + " have been evaluated as " + status.Text() + " by the IUCN. ";
                }
            }

            int subpops_species = cr_stats.subpops_species;
            int subpops_subspecies = cr_stats.subpops_subspecies;
            string subpops_species_text = subpops_species.NewspaperNumber() + " species subpopulation" + (subpops_species == 1 ? "" : "s");
            string subpops_subspecies_text = subpops_subspecies.NewspaperNumber() + " subspecies subpopulation" + (subpops_subspecies == 1 ? "" : "s");
            string have = (cr_stats.subpops_total == 1) ? "has" : "have";

            if (subpops_subspecies == 0) {
                //return "Of the " + node.nodeName.Adjectivize(false, false, "subpopulations", "within") + " evaluated by the IUCN, " + subpops_species_text + " " + have + " been assessed as " + status.Text() + ".";
                return "Of the subpopulations of " + node.nodeName.LowerPluralOrTaxon() + " evaluated by the IUCN, " + subpops_species_text + " " + have + " been assessed as " + status.Text() + ".";
            } else {
                //return "Of the " + node.nodeName.Adjectivize(false, false, "subpopulations", "within") + " evaluated by the IUCN, " + subpops_species_text + " and " + subpops_subspecies_text + " " + have + " been assessed as " + status.Text() + ".";
                return "Of the subpopulations of " + node.nodeName.LowerPluralOrTaxon() + " evaluated by the IUCN, " + subpops_species_text + " and " + subpops_subspecies_text + " " + have + " been assessed as " + status.Text() + ".";
            }
        }

        public static string ThreatenedContextBlurb(TaxonNode node, RedStatus status) {

            if (status == RedStatus.EN) {
                string critCaption = RedStatus.CR.TextWithRecently() + " " + node.nodeName.LowerPluralOrTaxon();
                //string critLink = "List of " + critCaption;
                string critLink = node.nodeName.ListOf(RedStatus.CR); // "List of " + cr_caption;

                var combined_species_count = node.GetStats(RedStatus.EN).species + node.GetStats(RedStatus.CR).species;

                string blurb = "For a species to be considered endangered by the IUCN it must meet certain quantitative criteria which are designed to classify taxa facing \"a very high risk of exintction\". " 
                + "An even higher risk is faced by ''critically endangered'' species, which meet the quantitative criteria for endangered species. [[" + critLink + "|" + critCaption.UpperCaseFirstChar() + "]] are listed separately. " 
                + "There are " + combined_species_count.NewspaperNumber() + " " + node.nodeName.Adjectivize(false, false, "species") + " which are endangered or critically endangered. ";

                return blurb.TrimEnd(); ;
                // mention threatened / vu too?: Threatened species are those which fall into the categories of vulnerable, endangered, or critically endangered.

            } else if (status == RedStatus.VU) {
                //string cr_caption = RedStatus.CR.TextWithRecently() + " " + node.nodeName.LowerPluralOrTaxon();
                string cr_link = node.nodeName.ListOf(RedStatus.CR); // "List of " + cr_caption;
                //string en_caption = RedStatus.EN.TextWithRecently() + " " + node.nodeName.LowerPluralOrTaxon();
                string en_link = node.nodeName.ListOf(RedStatus.EN); //"List of " + en_caption;

                string blurb = "For a species to be assessed as vulnerable to extinction the best available evidence must meet quantitative criteria set by the IUCN designed to reflect \"a high risk of extinction in the wild\". "
                    + "''Endangered'' and ''critically endangered'' species also meet the quantitative criteria of ''vulnerable'' species, and are listed separately. See: [[" + en_link + "]], [[" + cr_link + "]]. "
                    + "Vulnerable, endangered and critically endangered species are collectively referred to as ''[[threatened species]]'' by the IUCN. ";

                //TODO: something less awkward, like
                //+ "Vulnerable fishes have the lowest risk assessment of the threatened species, which also includes endangered fishes and critically endangered fishes."

                return blurb.TrimEnd();
            }

            // meh: While endangered and critically endangered species also meet the IUCN criteria of "vulnerable species", they are listed separately due to their higher risk of exinction. There are xx species in the three categories combined (i.e. threatened species). See: [[List of ...]] and [[List of...]]

            return null;
        }


        public static string DDInfo(TaxonNode node, RedStatus status) {
            if (!status.isThreatened())
                return string.Empty;

            var dd_stats = node.GetStats(RedStatus.DD);
            var all_stats = node.GetStats();

            int dd_sp = dd_stats.species;
            int evaluated_sp = all_stats.species;

            if (dd_sp == 0)
                return string.Empty;

            string thismany = dd_sp.NewspaperNumber() + " " + node.nodeName.Adjectivize(false, false, "species", "in");
            string percent = Percent(dd_sp, evaluated_sp);

            string ddinfo = "Additionally " + thismany + " (" + percent + " of those evaluated) are listed as ''[[data deficient]]'', meaning there is insufficient information for a full assessment of conservation status. As these species typically have small distributions and/or populations, they are intrinsically likely to be threatened, according to the IUCN.<ref>{{cite web|title=Limitations of the Data|url=http://www.iucnredlist.org/initiatives/mammals/description/limitations|website=The IUCN Red List of Threatened Species|publisher=Union for Conservation of Nature and Natural Resources (IUCN)|accessdate=11 January 2016}}</ref> While the category of ''data deficient'' indicates that no assessment of extinction risk has been made for the taxa, the IUCN notes that it may be appropriate to give them \"the same degree of attention as threatened taxa, at least until their status can be assessed.\"<ref>{{cite web|title=2001 Categories & Criteria (version 3.1)|url=http://www.iucnredlist.org/static/categories_criteria_3_1|website=The IUCN Red List of Threatened Species|publisher=Union for Conservation of Nature and Natural Resources (IUCN)|accessdate=11 January 2016}}</ref> ";
            //ddinfo += "\n\n";

            // TODO: add for amphibians in particular
            // http://www.iucnredlist.org/initiatives/amphibians/analysis/red-list-status
            // "It is predicted that a significant proportion of these Data Deficient species are likely to be globally threatened."

            return ddinfo;
        }

        public static string LastParagraphNotes(TaxonNode node, RedStatus status) {
            // final paragraph:

            // This is a complete list of critically endangered mammalian species and subspecies as evaluated by the IUCN. 
            // Species considered possibly extinct by the IUCN are marked as such. 
            // Species or subspecies which have critically endangered subpopulations (or stocks) are indicated. 
            // Where possible common names for taxa are given while links point to the scientific name used by the IUCN.

            var breakoutNodes = node.breakoutNodes;
            var style = node.GetStyle();

            string thisIsaListOf = "This is a complete list of ";
            string exceptions = string.Empty;
            if (breakoutNodes != null && breakoutNodes.Count > 0) {
                //TODO
                thisIsaListOf = "This is a list of all ";
                //exceptions = ", except for blah and blah which are listed separately.";
            }

            var stats = node.GetStats(status);
            bool isPlants = node.IsOrParentIs("Plantae");

            StringBuilder note = new StringBuilder();

            string whats = "species and subspecies";
            if (stats.subspecies == 0) {
                whats = "species";
            } else if (isPlants && stats.subspecies_varieties > 0) {
                whats = "species, subspecies and varieties";
            }

            string listOfWhat;
            if (status == RedStatus.Null) {
                listOfWhat = node.nodeName.Adjectivize(false, false, whats, "in");
            } else {
                listOfWhat = status.Text() + " " + node.nodeName.Adjectivize(false, false, whats, "in");
            }
            note.Append(thisIsaListOf + listOfWhat + " evaluated by the IUCN. "); // "evaluated" or "as evaluated"?

            if (status == RedStatus.CR) {
                note.Append("Species considered possibly extinct by the IUCN are marked as such. ");
            }

            if (status != RedStatus.Null && status != RedStatus.EXplus && status != RedStatus.EX
                    && stats.subpops_total > 0) {
                note.Append(whats.UpperCaseFirstChar() + " which have " + status.Text() + " subpopulations (or stocks) are indicated. ");
            }

            if (style == PrettyStyle.JustNames) {
                //note.Append("Common names for taxa are displayed where possible. Links generally point to the scientific name used by the IUCN. ");
                note.Append("Where possible common names for taxa are given while links point to the scientific name used by the IUCN. ");
            }

            return note.ToString().TrimEnd();
        }
    }

}