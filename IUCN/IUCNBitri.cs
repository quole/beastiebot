﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace beastie {

    // A binomial or trinomial, with optional stock/population, and threatened status
    // TODO: make into a composite containing a generic Bitri (+ stockpop + status + special status)
    // TODO regions?

    public class IUCNBitri : ICloneable // was: Bitri
    {
        //enum Kingdom { None, Plant, Animal, Fungi } // etc...

        //▿

        //enum type { None, binom, trinom }
        public bool isStockpop {
            get {
                return !string.IsNullOrEmpty(stockpop);
            }
        }

        public bool hasSubgenus {
            get {
                return !string.IsNullOrEmpty(subgenus);
            }
        }

        public bool isTrinomial {
            get {
                return (!string.IsNullOrEmpty(epithet) && !string.IsNullOrEmpty(infraspecies));
            }
        }

        public bool isVariety {
            get {
                return (NormalizedInfrarank() == "var.");
            }
        }

        // binomial, not a trinomial, not a stockpop.
        public bool isSpecies
        {
            get
            {
                return !isStockpop && !isTrinomial;
            }
        }

        public bool isSubspeciesOrVariety {
            get {
                return !isStockpop && isTrinomial;
            }
        }

        public bool isSubspeciesNotVariety {
            get {
                return !isStockpop && isTrinomial && !isVariety;
            }
        }

        // Ootaxa: Template:Oobox
        // Ichnotaxa: Template:Ichnobox
        // Excavata (Domain: Eukaryota)
        // Rhizaria (unranked) (Domain: Eukaryota)
        // Chromalveolata (Domain: Eukaryota) (polyphyletic)
        enum Kingdom_Taxobox { None, Animalia, Archaeplastida, Fungi, Chromalveolata, Rhizaria, Excavata, Amoebozoa, Bacteria, Archaea, Viruses, incertae_sedis, Ichnotaxa, Ootaxa }
        enum Kingdom_COL { None, Animalia, Archaea, Bacteria, Chromista, Fungi, Plantae, Protozoa, Viruses }
        public enum Kingdom_IUCN { None, Animalia, Bacteria, Chromista, Fungi, Plantae, Protozoa }

        public Kingdom_IUCN kingdom;

        public string iucnId;

        public string genus;
        public string subgenus;
        public string epithet;
        public string infrarank; // infraspecific rank, e.g. ssp. subsp. var. 
        public string connecting_term; // from above
        public string infraspecies; // e.g. subspecies or variety

        public string stockpop; // stock/subpopulation
        public bool multiStockpop; // if this Bitri represents multiple Stocks/Subpopulation. If true, "stockpop" must contain a description, e.g. "1 stock or subpopulation" or "3 subpopulations", and redlistStatus should only be set if all members are the same

        public string CommonNameEng; // list of comma separated common names from the IUCN

        public string FirstCommonNameEng() {
            if (string.IsNullOrEmpty(CommonNameEng))
                return null;

            var names = CommonNamesEng();

            if (names.Length >= 1) {
                return names[0];
            }

            return null;
        }

        public string[] CommonNamesEng() {
            if (string.IsNullOrEmpty(CommonNameEng))
                return null;

            // Fail if any numbers in common name field. Field may have been used for incorrectly for <Author, Year>
            if (CommonNameEng.Any(char.IsNumber))
                return null;
            
            return CommonNameEng.Split(new char[] { ',' })
                .Select(m => m.Trim())
                .Select(m => Regex.Replace(m, @"^The ", "", RegexOptions.IgnoreCase)) // remove starting "The "
                .Select(m => Regex.Replace(m, @"\.$", "")) // remove trailing dot (.)
                .Select(m => Regex.Replace(m, @" \(fb\)", "", RegexOptions.IgnoreCase)) // remove trailing "(fb)" (fishbase names)
                .Select(m => m.Replace("chamaeleon", "chameleon")) // use English spelling rather than Latin
                .Select(m => m.Replace("Chamaeleon", "Chameleon"))
                .Select(m => m.Replace("  ", " ")) // remove double spaces
                .Select(m => m.Replace("*", "")) // e.g. "*Heuglin's Gazelle", "* Mongalla Gazelle"
                .Select(m => m.Replace('´', '\'')) // replace acute accent (´) with apostrophe ('). e.g. Mops trevori (Trevor´s Bat)
                .Select(m => m.Trim()) // trim again
                .Where(m => m != string.Empty && !m.ToLowerInvariant().StartsWith("species code")) // ignore "species code:" entries. e.g. Halophila engelmanni. "Species code: He, Stargrass"
                .ToArray();
        }

        public string BestCommonNameEng() {
            string[] names = CommonNamesEng();
            if (names == null)
                return null;

            if (names.Length == 1)
                return names[0];

            //find a name without any apostrophe (to avoid eponymous names)
            string best = names.Where(n => !n.Contains("'")).FirstOrDefault();
            if (best != null)
                return best;

            return names[0]; // ok just the first one then.
        }


        public RedStatus Status; // should never be RedStatus.Null. use None or Unknown instead.

        TaxonPage _taxonName; // cached // (use TaxonName instead?)

        public IUCNBitri() {
		}

        // should we hide the infrarank? (i.e. yes for animal subspecies which are written with "ssp.", no otherwise)
        // no longer used
        // use NormalizedInfrarank() instead
        public bool shouldInfrarankBeVisible {
			get {
				if (string.IsNullOrEmpty(infraspecies)) // no infraspecies, no infrarank
					return false;

				if (string.IsNullOrEmpty(infrarank))
					return false;

				if (kingdom == Kingdom_IUCN.Animalia && (infrarank == "ssp." || infrarank == "subsp."))
					return false;

				return true;
			}
		}

        /// <summary>
        /// Normalize the infrarank. 
        /// For animals, no infrarank should be given for subspecies. (There shouldn't be any others, but let them pass)
        /// For plants, always use "subsp." 
        /// 
        /// IUCN database has a mix of "ssp." and "subsp." for plants. Sometimes for the same genus.
        /// e.g. Clermontia oblongifolia ssp. mauiensis
        ///      Clermontia samuelii subsp. hanaensis
        ///      
        /// TODO: could do further checking, e.g. for odd capitals etc, but should be fine
        /// TODO: add (per kingdom) ssp. vs subsp. counts to a report 
        /// </summary>
        /// <returns>The normalized infrarank ("subsp." or "var.") or an empty string.</returns>
        public string NormalizedInfrarank() {
            if (string.IsNullOrEmpty(infraspecies)) // no infraspecies, no infrarank
                return string.Empty;

            if (string.IsNullOrEmpty(infrarank))
                return string.Empty;

            if (kingdom == Kingdom_IUCN.Animalia && (infrarank == "ssp." || infrarank == "subsp."))
                return string.Empty;

            if (kingdom == Kingdom_IUCN.Plantae && infrarank == "ssp.")
                return "subsp.";

            return infrarank;
        }

        /**
		 * Excludes status
		 * Excludes "ssp." infrarank label for animals (unless normalizeInfraRank = false)
         * Uses "subsp." instead of "ssp." for plants
		 * Excludes stock/subpopulation
		 * 
		 * used for matching IUCN names with TaxonDisplayRules
         * 
         * Only turn off "normalizeInfraRank" when printing for debugging
		 */
        public string BasicName(bool normalizeInfraRank = true) {
			// copied from TaxonDetails.FullSpeciesName()
			// some weird species have infra-ranks but not epithets (e.g. sp. nov.)
			string speciesString = "";
			if (!string.IsNullOrEmpty(epithet)) {
				speciesString = string.Format(" {0}", epithet);
			}

			string infraString = "";
			if (!string.IsNullOrEmpty(infraspecies)) {
                string infrank = normalizeInfraRank ? NormalizedInfrarank() : infrarank;

                if (string.IsNullOrEmpty(infrank)) {
                    infraString = string.Format(" {0}", infraspecies);
                } else {
                    infraString = string.Format(" {0} {1}", infrank, infraspecies);
                }
			}

			return string.Format("{0}{1}{2}", genus, speciesString, infraString);
		}

		/** includes stock/pop. For debug.
		TODO: include subgenus
		 */
		public string FullDebugName() {
            bool normalizeInfraRank = false;
			//string pop = "";
			if (!string.IsNullOrEmpty(stockpop)) {
				return string.Format("{0} ({1})", BasicName(normalizeInfraRank), stockpop);
			} else {
				return BasicName(normalizeInfraRank);
			}
		}

        public bool isIdNull() {
            if (string.IsNullOrWhiteSpace(iucnId)) return true;
            if (iucnId == "0") return true;
            return false;
        }

        public string NameLinkIUCN() {
            if (isIdNull())
                return FullDebugName();

            string urlFmt = @"http://www.iucnredlist.org/details/{0}/0";
            string url = string.Format(urlFmt, iucnId);
            return "[" + url + " " + FullDebugName() + "]";
        }


        public string ShortBinomial() {
			// some weird species have infra-ranks but not epithets (e.g. sp. nov.)
			string speciesString = "";
			if (!string.IsNullOrEmpty(epithet)) {
				speciesString = string.Format(" {0}", epithet);
			} else {
				// only use infraspecies + infrarank if missing epithet
				if (!string.IsNullOrEmpty(infraspecies)) {
					if (!string.IsNullOrEmpty(infrarank)) {
						speciesString = string.Format(" {0} {1}", infrarank, infraspecies);
					} else {
						speciesString = string.Format(" {0}", infraspecies);
					}
				}
			}

			return string.Format("{0}{1}", genus, speciesString);
		}
		public IUCNBitri CloneMultistockpop(string stockpopText, bool keepStatus = false) {
			IUCNBitri clone = (IUCNBitri) Clone();
			clone.multiStockpop = true;
			clone.stockpop = stockpopText;
			if (!keepStatus) 
				clone.Status = RedStatus.None;

			return clone;
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}

        public TaxonPage TaxonName() {
            if (_taxonName == null) {
                _taxonName = BeastieBot.Instance().GetTaxonNamePage(this);
            }
            
            return _taxonName;
        }

    }
}

