Learn MongoDB By Comparison
===========================

0) Prepare the data 
mongoimport patflt1-15000.json -d patfltdb -c patflt --drop /v
mongoimport patflt15001-30000.json -d patfltdb -c patflt /v
mongoimport patflt30001-45000.json -d patfltdb -c patflt /v
mongoimport patflt45000-55430.json -d patfltdb -c patflt /v


1) Find
SQL> select * from dcwrkdta/patflt 

use patfltdb

db.patflt.find()
db.patflt.find().pretty()


2) Count
SQL> select count(*) from dcwrkdta/patflt  

db.patflt.find().count()

SQL> select count(*) as total from dcwrkdta/patflt where bldcod='WP'and blocod='01'  

db.patflt.find({"BLDCOD":"WP", "BLOCOD" : 1}).count() 

SQL> SELECT count(*) FROM dcwrkdta/patflt WHERE usfare >= 100    

db.patflt.find({ USFARE: { $gte: 100}}).count()


3) $match
SQL> select * from dcwrkdta/patflt where bldcod='WP'and blocod='01'  

db.patflt.aggregate([ 
		{ $match : { BLDCOD : 'WP', BLOCOD : 1 } } 
	]).pretty()  


4) $match + $count
SQL> select count(*) as total from dcwrkdta/patflt where bldcod='WP'and blocod='01'    

db.patflt.aggregate([ 
		{ $match : { BLDCOD : 'WP', BLOCOD : 1 } },
		{ $count : 'total' }
	])


5) $match + $project 
SQL> SELECT BLDCOD, BLOCOD, FLTCOD, UNTCOD FROM dcwrkdta/patflt 
	WHERE bldcod='WP'and blocod='01'         
    
db.patflt.aggregate([
	  { $match : { BLDCOD : 'WP', BLOCOD : 1 } }, 
	  { $project : { _id : 0, 'BLDCOD' : 1, 'BLOCOD' : 1, 'FLTCOD' : 1, 'UNTCOD' : 1 } }
]).pretty()


6) $match + $project + $out 
SQL> create table wp01 as 
	(SELECT PARCOD, SECCOD, BLDCOD, BLOCOD, FLTCOD, UNTCOD 
	FROM dcwrkdta/patflt WHERE bldcod='WP'and blocod='01' )
	
db.patflt.aggregate([
	  { $match : { BLDCOD : 'WP', BLOCOD : 1 } }, 
	  { $project : { _id : 0, 'BLDCOD' : 1, 'BLOCOD' : 1, 'FLTCOD' : 1, 'UNTCOD' : 1 } },
	  { $out : 'wp01'}
	])	  


7) $group + $sort (single)
SQL> SELECT PARCOD, count(*) as total 
	from dcwrkdta/patflt      
	group by PARCOD           
	order by count(*) desc    

db.patflt.aggregate([
    {"$group" : {_id:"$PARCOD", total:{$sum:1}}}, 
	{ $sort : { 'total' : -1 } }
])


8) $group + $sort (multiple)
SQL> SELECT PARCOD, SECCOD, BLDCOD, BLOCOD, count(*) as total
	from dcwrkdta/patflt                             
	group by PARCOD, SECCOD, BLDCOD, BLOCOD          
	order by 1, 2                                    

db.patflt.aggregate([
	{ "$group" : {_id: {
						"PARCOD": "$PARCOD",
						"SECCOD": "$SECCOD",
						"BLDCOD": "$BLDCOD",
						"BLOCOD": "$BLOCOD",
	  }, total:{$sum:1}
	}
	}, 
	{ 
	"$sort" : { '_id.PARCOD' : 1, "_id.SECCOD": 1, "_id.BLDCOD" : 1, "_id.BLOCOD" : 1 } 
	}
])


9) $lookup
SQL> select f1.parcod, f1.seccod, f1.bldcod,  
			f1.blocod, f1.fltcod, f1.untcod,  
		   (select bmbldnmc                   
			from dcwrkdta/bmbdg f2            
			where f1.parcod=f2.bmparcod and   
				  f1.seccod=f2.bmseccod and   
				  f1.bldcod=f2.bmbldcod)      
	from dcwrkdta/patflt f1, dcwrkdta/bmbdg f2
	where f1.bldcod='WP' and f1.blocod='01'   
	order by f1.parcod, f1.seccod, f1.bldcod, 
			 f1.blocod, f1.fltcod, f1.untcod  

db.patflt.aggregate([
		{ $match : { BLDCOD : 'WP', BLOCOD : 1 } },
		{ $project : { 
						_id : 0, 
						'PARCOD' : 1, 'SECCOD': 1, 'BLDCOD' : 1, 
						'BLOCOD' : 1, 'FLTCOD' : 1, 'UNTCOD' : 1
					 }
		},
		{ $lookup : {
						from : 'bmbdg',
						localField : 'PARCOD',
						localField : 'SECCOD',
						localField : 'BLDCOD',
						foreignField : 'BMPARCOD',
						foreignField : 'BMSECCOD',
						foreignField : 'BMBLDCOD',
						as : 'bmbdg'
					}
		},
		{ $unwind : '$bmbdg' },
		{ $project : { 
						_id : 0, 
						'PARCOD' : 1, 'SECCOD': 1, 'BLDCOD' : 1, 
						'BLOCOD' : 1, 'FLTCOD' : 1, 'UNTCOD' : 1,
						'BMBLDNMC' : '$bmbdg.BMBLDNMC',
						'BMBLDNMP' : '$bmbdg.BMBLDNMP'
					 }
		}
]).pretty()


10) forEach + temp file
db.temp.drop()
db.patflt.find({"BLDCOD":"WP", "BLOCOD" : 1}).forEach( 
	function (doc)
	{
		//print (doc.PARCOD, doc.SECCOD, doc.BLDCOD)
		let BMBDG=db.bmbdg.findOne({BMPARCOD: doc.PARCOD, BMSECCOD: doc.SECCOD, BMBLDCOD: doc.BLDCOD})
		//print (BMBDG.BMBLDNMC)	
		let newDoc = {}
		newDoc.PARCOD = doc.PARCOD
		newDoc.SECCOD = doc.SECCOD
		newDoc.BLDCOD = doc.BLDCOD
		newDoc.BLOCOD = doc.BLOCOD
		newDoc.FLTCOD = doc.FLTCOD
		newDoc.UNTCOD = doc.UNTCOD
		
		newDoc.BMBLDNMC = BMBDG.BMBLDNMC
		newDoc.BMBLDNMP = BMBDG.BMBLDNMP
		db.temp.insert(newDoc);
	}
)
db.temp.find().pretty()


11) query + project + sort 
SQL> SELECT BMPARCOD, BMSECCOD, BMBLDCOD, BMBLDNMC, BMBLDNMP 
     FROM   
     dcwrkdta/bmbdg 
	 ORDER BY BMPARCOD, BMSECCOD, BMBLDCOD           

db.bmbdg.find({}, {_id:0, BMPARCOD:1, BMSECCOD:1, BMBLDCOD:1, BMBLDNMC:1, BMBLDNMP:1}).
	sort({BMPARCOD:1, BMSECCOD:1, BMBLDCOD:1})


12) query + project + sort 
SQL> SELECT BMPARCOD, BMSECCOD, BMBLDCOD, BMBLDNMC, BMBLDNMP 
     FROM dcwrkdta/bmbdg                                                
     where bmbldcod in ('WL', 'WP')                                
     ORDER BY BMPARCOD, BMSECCOD, BMBLDCOD                         

db.bmbdg.find({BMBLDCOD: {$in: ['WL', 'WP'] }}, 
			  {_id:0, BMPARCOD:1, BMSECCOD:1, BMBLDCOD:1, BMBLDNMC:1, BMBLDNMP:1}).
	sort({BMPARCOD:1, BMSECCOD:1, BMBLDCOD:1}).pretty()


13) $max 
SQL> SELECT max(RAWARE) FROM dcwrkdta/patflt

db.patflt.find().sort({RAWARE:-1}).limit(1).pretty()

db.patflt.find().sort({RAWARE:-1}).limit(1).forEach(function (doc) { print(doc.RAWARE) })

db.patflt.find().sort({RAWARE:-1}).limit(1).toArray().map(function(doc){return doc.RAWARE})

db.patflt.aggregate([{
	$group : { _id: null, max: { $max : "$RAWARE" }}
}])


14. distinct 
SQL> select distinct PARCOD 
     from dcwrkdta/patflt   

db.patflt.distinct('PARCOD')


15) Insert with types 
SQL> insert into dcwrkdta/tbrelcod (relcod, reldes, reldesc, update_ident) 
     values('01', 'REPRESENTANTE', '家團代表', 2)

db.tbrelcod.insert({
					 relcod: '01',
					 reldes: 'REPRESENTANTE',
					 reldesc: '家團代表', 
					 update_ident: 2
				   })  
db.tbrelcod.insert({
					 relcod: String(02),
					 reldes: 'PROMITENTE COMP.',
					 reldesc: '第二承諾業主', 
					 update_ident: 2
				   }) 


16) Use the following JavaScript expression in the Query

db.tbrelcod.find("parseInt(this.relcod)==1").pretty() 
db.tbrelcod.find("this.relcod=='2'").pretty()


17) Index 
db.tbrelcod.createIndex(
  {
      relcod: 1
  },
  {
      name: 'tbrelcod01',
      unique: true
  }
)

db.tbrelcod.getIndexes()
db.tbrelcod.dropIndex('tbrelcod01')

/*
   keys
   A document that contains the field and value pairs where the field is the index key and 
   the value describes the type of index for that field. 
   For an ascending index on a field, specify a value of 1; 
   for descending index, specify a value of -1. 
   
   name 
   Optional. The name of the index. If unspecified, MongoDB generates an index name by 
   concatenating the names of the indexed fields and the sort order
   
   unique 
   Optional. Creates a unique index so that the collection will not accept insertion or 
   update of documents where the index key value matches an existing value in the index.
*/


18) Mongo Atlas 
Login in with Google Account 

Database Access:
	username:netninja
	password:s82mZny3jHHXQx0v
	database:nodetuts

a) Mongo Shell: 
Login shortcut: 
C:\laragon\bin\mongodb\mongodb-4.0.3\mongo "mongodb+srv://nodetuts.u39op.mongodb.net/nodetuts" --username netninja --password s82mZny3jHHXQx0v

b) Robo 3T
	i) Add connection 
	ii) Paste to text box next to "From SRV" button
	mongodb+srv://netninja:s82mZny3jHHXQx0v@nodetuts.u39op.mongodb.net/nodetuts
	iii) Press "From SRV" button 
	iv) Press "Test" button 
	v) Name the connection and press "Save"


Reference: 
1. How to Use the mongoimport to Import a JSON file into the local MongoDB server
   https://www.mongodbtutorial.org/mongodb-crud/mongoimport/

2. MongoDB SELECT COUNT GROUP BY
   https://stackoverflow.com/questions/23116330/mongodb-select-count-group-by

3. mongodb group values by multiple fields
   https://stackoverflow.com/questions/22932364/mongodb-group-values-by-multiple-fields
   
4. Can I use $project to return a field as the top level document in a mongo aggregation query?
   https://stackoverflow.com/questions/19434237/can-i-use-project-to-return-a-field-as-the-top-level-document-in-a-mongo-aggreg   

5. How do I perform the SQL Join equivalent in MongoDB?
   https://stackoverflow.com/questions/2350495/how-do-i-perform-the-sql-join-equivalent-in-mongodb

6. MongoDB: How to Find the Max Value in a Collection
   https://www.statology.org/mongodb-max-value/
   
7. “mongodb get maximum value from collection” Code Answer’s
   https://www.codegrepper.com/code-examples/whatever/mongodb+get+maximum+value+from+collection
   
8. MongoDB - Data Types   
   https://www.w3schools.in/mongodb/data-types/#Integer
   
9. Is it possible to cast in a MongoDB-Query?
   https://stackoverflow.com/questions/3521601/is-it-possible-to-cast-in-a-mongodb-query/14350204

10. db.collection.createIndex()
    https://docs.mongodb.com/manual/reference/method/db.collection.createIndex/
   
11. Indexing Strategies
    https://docs.mongodb.com/manual/applications/indexes/
	

By Albertoi on 2022/01/21 
<EOF>
