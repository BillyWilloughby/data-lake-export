select 
/*Top doesn't work here, I suppose it's because of the CTE*/
--CONO, DIVI, WHLO, CompanyLag, DivisionLag, WarehouseLag,
RowNumber,
CASE WHEN CompanyLag is null or CompanyLag != CONO then RTRIM(CONO) else '' end Company, 
CASE WHEN DivisionLag is null or DivisionLag != DIVI then RTRIM(DIVI) else '' end Division, 
CASE WHEN WarehouseLag is null or WarehouseLag != WHLO then RTRIM(WHLO) else '' end Warehouse, 
"Item Number",
"Available Qty",
"On Order Qty"

 from (
select
top 2000 --Put TOP HERE
ROW_NUMBER ( )   
    OVER ( Order By CONO, DIVI, WHLO, ITNO)  RowNumber,
CAST(LAG(MB.CONO,1) OVER (ORDER BY MB.CONO, MB.DIVI, MB.WHLO, MB.ITNO) AS INT) CompanyLag, 
CAST(LAG(MB.DIVI,1) OVER (ORDER BY MB.CONO, MB.DIVI, MB.WHLO, MB.ITNO) AS CHAR(3)) DivisionLag,
CAST(LAG(MB.WHLO,1) OVER (ORDER BY MB.CONO, MB.DIVI, MB.WHLO, MB.ITNO) AS VARCHAR(10)) WarehouseLag, 
MB.CONO, MB.DIVI, MB.WHLO, 
MB.ITNO "Item Number",
sum(MB.AVAL) "Available Qty", 
sum(MB.ORQT) "On Order Qty"

from MITBAL MB
where MB.deleted = 'false' AND MB.CONO =  080
GROUP BY MB.CONO, MB.DIVI, MB.WHLO, MB.ITNO
ORDER BY MB.CONO, MB.DIVI, MB.WHLO, MB.ITNO) cteTest
Order By 1