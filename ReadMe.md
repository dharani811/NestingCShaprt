This a port of Nesting library written in Java at https://github.com/exacloud/Nest4J

Not complete yet, but basics work.Working on it, will keep you posted.

i/p is XML format

o/p will be generated in SVG format

sample input file is available in bin folder of NestingConsole project.

Sample use case:




                        //create bin
            NestPath bin = new NestPath();
            double binWidth =1000;
            double binHeight = 1000;
            bin.add(0, 0);
            bin.add(binWidth, 0);
            bin.add(binWidth, binHeight);
            bin.add(0, binHeight);

			//read polygons from xml file
            var nestPaths = SvgUtil.transferSvgIntoPolygons("test.xml");

			//default Config, you can customize(please look into Config class)
            Config config = new Config();

			//Init Nesting
            Nest nest = new Nest(bin, nestPaths, config, 2);

			//perform nesting
            List<List<Placement>> appliedPlacement = nest.startNest();

			//convert to svg polygons for viewing in browser
            var svgPolygons =  SvgUtil.svgGenerator(nestPaths, appliedPlacement, binWidth, binHeight);

			//save file onto disk
            SvgUtil.saveSvgFile(svgPolygons, "output.svg");



UseFul Links:
https://github.com/exacloud/Nest4J

https://github.com/Jack000/Deepnest

https://github.com/Jack000/SVGnest
