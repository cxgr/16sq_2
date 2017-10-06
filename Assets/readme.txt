Unzip the models folder anywhere outside of project folder, modify the default path as needed
If you want to replace the models, limitations are:
- loader will only look for obj files with this set of names (1 - 5)
- model should be a single triangulated mesh (submeshes are ignored), with one material only, poly count below 64999
- models are imported with face normals, uv data and albedo map, everything else is ignored

Tested with Maya 2014 obj exporter format