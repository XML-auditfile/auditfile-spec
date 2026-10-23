13-2-2017: SavingsUnit moet ook negatief kunnen zijn: van nonNegativeInteger naar Integer (op 2 plaatsen).
15-3-2017: Enkele optionele totalen opgenomen om controles uit te kunnen voeren:
		- Cashregister: totTransAmntIn, totTransAmntEx
		- Cashtransaction: totVatAmnt, totDscAmnt, totRaiseAmnt, totPaidAmnt
		- ctLine: totLineDscAmnt, totLineRaiseAmnt
De manier van verwijzen naar een RGS taxonomie en element in lijn brengen met XAF3.3.
(velden RGSVersion en LeadReference vervallen, Onder GeneralLedgerAccount is nu de Taxonomy met details opgenomen)
27-3-2017: Nieuwe ISO Valutacode 'RON' opgenomen.
27-03-2017 ISO codelijsten in eigen schema's ondergebracht en als import in het XAF schema opgenomen.

Engelstalig maken van omschrijvingen bij een aantal codewaarden uit de code list:  Predefined basic ID
23001	Renter
23999	Other
26001	Square meter
27001	Real-estate (NL: WOZ) value
27002	Sales value
27003	Gross value
27004	Nett value
28001	Taxed
28002	Tax-free
31001	Stock increase
31002	Stock decrease
31003	Absolute stock in trade for export for inventory level exchange
31004	Receipt of goods
31005	Storage of goods
31006	Removal of goods
31007	Transferral of goods
31008	Change increase
31009	Change decrease
32001	Not condensed
32002	Condensed
