select 
top 10000 
	OD.CONO Company,
	OD.PRRF PriceList,
	HM.STYN Style,
	OD.ITNO SKU,
	BUAR Brand,
	ITGR GenderID,
	CT3.TX15 Gender_Desc,
	PRGP Category1ID, 
	CT1.TX15 Category1_Desc,
	MM.PDLN Category2ID,
	PL.TX15 Category2_Desc, 
	DIM1 Measure1, 
	DIM2 Measure2, 
	DIM3 Measure3, 
	HM.TY15 Color, 
	HM.TX15 Size,
	coalesce(MP.POPN, '') UPC, 
	cast (OD.SAPR as decimal(12,4)) Price, 
	OD.CUCD Currency, 
	OD.SPUN UOM 
 from
  OPRBAS OD 
  join MITMAS MM on OD.CONO = MM.CONO and OD.ITNO = MM.ITNO and MM.STAT = '20' and MM.deleted = 'false'
  join MITMAH HM ON HM.CONO = OD.CONO and HM.ITNO = OD.ITNO and OD.deleted = 'false'
  join CSYTAB CT1 on CT1.CONO = OD.CONO and CT1.STCO = 'PRGP' and CT1.STKY = PRGP and CT1.deleted = 'false'
  join CSYTAB CT3 on CT3.CONO = OD.CONO and CT3.STCO = 'ITGR' and CT3.STKY = ITGR and CT3.deleted = 'false'
  join CRPDLN PL  on  PL.CONO = OD.CONO and PL.PDLN = MM.PDLN and PL.deleted = 'false'
  left join MITPOP MP on MP.ALWQ = 'UPC' and MP.CONO = OD.CONO and MP.ITNO = OD.ITNO and OD.deleted = 'false'
where OD.CONO = 100
order by 1,2,3