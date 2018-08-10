

use finn
--GSXR 750
declare @nypris int =170000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%suzuki%'
and (modell like '%gsx%' and modell like '%r%')
and slagvolum between 700 and 800
and effekt >120
order by sistEndret desc


--GSXR 1000
set @nypris =260000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%suzuki%'
and (modell like '%gsx%' and modell like '%r%')
and slagvolum between 900 and 1200
and effekt >120
order by sistEndret desc

--GSXR 600
set @nypris =160000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0 as pris,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%suzuki%'
and (modell like '%gsx%' and modell like '%r%')
and slagvolum between 550 and 670
and effekt >100
order by sistEndret desc

--Triumph speed tripple
set @nypris =200000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0 as pris,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%triumph%'
and (modell like '%speed%' and modell like '%triple%')
and slagvolum between 1000 and 1100
order by sistEndret desc


--Ducati 1098
set @nypris  =300000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0 as pris,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%ducati%'
and (modell like '%1098%' and modell like '%1098%')
and slagvolum between 1000 and 1100
order by sistEndret desc

--Kavasaki
set @nypris  =200000
select finnid,merke,modell,kmstand,årsmodell,(year(getdate())+1-årsmodell)as antallår,effekt,pris*1.0 as pris,@nypris as nypris,@nypris-pris as verdifall,(@nypris-pris)/(year(getdate())+1-årsmodell) as verdifallprår,((@nypris-pris)/(year(getdate())+1-årsmodell*1.0)*100/@nypris) as verdifallprosentprår,(@nypris-pris)/kmstand*1.0 as verdifallprkm, sistendret, url from annonserevisjon
where merke like '%kawasaki%'
and (modell like '%z%' and(modell like '%750%' or modell like '%900%' or modell like '%1000%'))
and slagvolum between 700 and 1000
and årsmodell>2005
order by sistEndret desc


--Pris er på vei ned - ny revisjon med lavere pris
select finnid,merke,modell,årsmodell,
(select top 1 pris from annonseRevisjon r2 where r2.finnId=r1.finnid order by id asc)as førstepris,
(select top 1 pris from annonseRevisjon r2 where r2.finnId=r1.finnid order by id desc)as gjeldendepris,
(select max(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid)as maxpris,
(select min(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid)as minpris,
((select max(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid)-(select min(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid))*100/(select max(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid) prosentnedgang,
count(*) antallrevisjoner
from annonseRevisjon r1
where pris<>(select max(pris) from annonseRevisjon r2 where r2.finnId=r1.finnid)
group by finnId,merke,modell,årsmodell
having count(*)>1
order by finnid desc
