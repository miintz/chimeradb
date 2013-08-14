#ChimeraDB (WORK IN PROGRESS)#


Turn *any* class structure into a database and corresponding DbContext to access said database.

###General jabber###

**It is still in pre-alpha state, but i have confirmed it works with at least one kind of recursive structure with complex types and multiple variations of that structure with more levels, different types of content etc.**


The current commit (d8127e5cad) will probably compile but it wont work very well, this is because i cant upload the data have used to test it, and it still loads in the generated model in a very undesirable way. In its current state its nothing more than a fancy prove of concept. Short term plans:

- Redesign structure into something more readable
- Dynamic loading of generated models
- **Get my point across**
- Get a hold of some sample data **to help get my point across**

These are just 4 points to make it more user friendly, but truth is the model is only useful to generate a database with a single function call DbContext.Database.Create(), but it's not much use to retrieve data with. This is because i haven't yet found a generic way to create the model with the right relations, using the enigmatic Fluent API. I have an inkling of an idea how to do it, but it may be a while before i get there. But if your model only has 1:1 relations then be my guest, it might just work in this state! 

###Mono support###

As of now ChimeraDB **doesn't** work on Mono. It's okay now, dry your tears and let me explain. I'm using Visual Studio's DTE object to load in the generated model. Now, i don't strictly need this object anyway but this object is not available in MonoDevelop. It would require some overhauling to make it work without the EnvDTE object. Also, System.Data.Entity is giving me nonsense, even though that should be part of Mono right? Anyway, i've read rumors about a MonoDevelop.Mono namespace which allows me to do roughly the same as EnvDTE, so a solution may be close by. 

But since i do not really think this is more important than first getting it to work properly, i won't just start on this yet.