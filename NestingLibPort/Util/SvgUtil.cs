using NestingLibPort.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


//package com.qunhe.util.nest.util;

//import com.qunhe.util.nest.data.NestPath;
//import com.qunhe.util.nest.data.Placement;
//import com.qunhe.util.nest.data.Segment;

//import java.io.File;
//import java.util.ArrayList;
//import java.util.List;
namespace NestingLibPort.Util
{
 public static  class SvgUtil
    {
        public static void saveSvgFile(List<String> strings,string path) 
        {
            StreamWriter f=null;
        if (!File.Exists(path) )
                
                {
                f = File.CreateText(path);
        }
        else
            {
                 File.Delete(path);
                f = File.CreateText(path);

            }

            f.Write("<?xml version=\"1.0\" standalone=\"no\"?>\n" +
                "\n" +
                "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \n" +
                "\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n" +
                " \n" +
                "<svg width=\"100%\" height=\"100%\" version=\"1.1\"\n" +
                "xmlns=\"http://www.w3.org/2000/svg\">\n");
        foreach(String s in strings){
                f.Write(s);
        }
            f.Write("</svg>");
        f.Close();
    }
        public static List<String> svgGenerator(List<NestPath> list, List<List<Placement>> applied, double binwidth, double binHeight)
        {
            List<String> strings = new List<String>();
        int x = 10;
        int y = 0;
        foreach (List<Placement> binlist in applied) {
            String s = " <g transform=\"translate(" + x + "  " + y + ")\">" + "\n";
        s += "    <rect x=\"0\" y=\"0\" width=\"" + binwidth + "\" height=\"" + binHeight + "\"  fill=\"none\" stroke=\"#010101\" stroke-width=\"1\" />\n";
            foreach (Placement placement in binlist) {
                int bid = placement.bid;
        NestPath nestPath = getNestPathByBid(bid, list);
        double ox = placement.translate.x;
        double oy = placement.translate.y;
        double rotate = placement.rotate;
        s += "<g transform=\"translate(" + ox + x + " " + oy + y + ") rotate(" + rotate + ")\"> \n";
                s += "<path d=\"";
                for (int i = 0; i<nestPath.getSegments().Count; i++) {
                    if (i == 0) {
                        s += "M";
                    } else {
                        s += "L";
                    }
Segment segment = nestPath.get(i);
s += segment.x + " " + segment.y + " ";
                }
                s += "Z\" fill=\"#8498d1\" stroke=\"#010101\" stroke-width=\"1\" />" + " \n";
                s += "</g> \n";
            }
            s += "</g> \n";
            y +=(int) (binHeight + 50);
            strings.Add(s);
        }
        return strings;
    }

    public static NestPath getNestPathByBid(int bid, List<NestPath> list)
{
    foreach (NestPath nestPath in list)
    {
        if (nestPath.bid == bid)
        {
            return nestPath;
        }
    }
    return null;
}


        public static List<NestPath> transferSvgIntoPolygons(string xmlFilePath) 
        {
            List<NestPath> nestPaths = new List<NestPath>();

            XDocument document = XDocument.Load(xmlFilePath);
            List<XElement> elementList = document.Root.DescendantNodes().OfType<XElement>().ToList();
        int count = 0;
        foreach (XElement element in elementList) {
            count++;
            if ("polygon"==(element.Name)) {
                    String datalist = element.Attributes((XName)"points").ToList()[0].Value.ToString();
        NestPath polygon = new NestPath();
                foreach (String s in datalist.Split(' ')) {
                    var temp = s.Trim();
                    if (temp.IndexOf(",") == -1) {
                        continue;
                    }
                    String[] value = s.Split(',');
        double x = Double.Parse(value[0]);
        double y = Double.Parse(value[1]);
        polygon.add(x, y);
                }
    polygon.bid = count;
                polygon.setRotation(4);
                nestPaths.Add(polygon);
            } else if ("rect"==element.Name) {
                double width = Double.Parse(element.Attributes((XName)"width").ToList()[0].Value.ToString());
double height = Double.Parse(element.Attributes((XName)"height").ToList()[0].Value.ToString());
double x = Double.Parse(element.Attributes((XName)"x").ToList()[0].Value.ToString());
double y = Double.Parse(element.Attributes((XName)"y").ToList()[0].Value.ToString());
NestPath rect = new NestPath();
rect.add(x, y);
                rect.add(x + width, y);
                rect.add(x + width, y + height);
                rect.add(x, y + height);
                rect.bid = count;
                rect.setRotation(4);
                nestPaths.Add(rect);
            }
        }
        return nestPaths;
    }


       
}
}
