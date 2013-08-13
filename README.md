#ChimeraDB (WORK IN PROGRESS)#


Turn *any* class structure into a database and corresponding DbContext to access said database.

** It is still in pre-alpha state, but i have confirmed it works with at least one kind of recursive structure with complex types and multiple variations of that structure with more levels, different types of content etc. **

The current commit (d8127e5cad) will not compile, this is because i cant upload the data have used to test it, and it still loads in the generated model in a very undesirable way. In its current state its nothing more than a fancy prove of concept. Short term plans:

- Redesign structure into something more readable
- Dynamic loading of generated models
- **Get my point across**
- Get a hold of some sample data **to help get my point across**

These are just 3 points to make it more user friendly, but truth is the model is only useful to generate a database with a single function call DbContext.Database.Create(), but it's not much use to retrieve data with. This is because i haven't yet found a generic way to create the model with the right relations. I have an inkling of an idea how to do it, but it may be a while before i get there.