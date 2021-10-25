<div id="top"></div>

<h3 align="center">Tool For Comparing KQL Queries</h3>

<div>
  <p align="center">
    Want to refactor a KQL query and check that the new version is compatible? <br>
    No more tedius manual comparing! This tool will judge if queries results are equvilant 
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>      
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#comparators">Comparators</a></li>
    <li><a href="#best-practices">Contributing</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

KQL refactoring can be motivated by many use cases including; <br> 
1) Performance improvements from rephrasing the original query <br>
2) Having to switch to a new table, that is built differently, and therefore requires to adjust the original query <br>

However, after refactoring comes the tedious task of verifying that there is no regression.
This tool purpose is to make this task and easy and hint what and where differences come from.

After successful demonstration in multiple use cases that prevented bugs and saved manual work, and interest from outside of the team, we release this tool so you can benefit from it too. 

<p align="right">(<a href="#top">back to top</a>)</p>


<!-- GETTING STARTED -->
## Getting Started

Tool creates based on original and refactored queries, a new query that determines if they return the same results and what differences there are if any. <br>
First comparing that there are the same number of results, and if so comparing columns value-by-value, according to customizable comparators. <br>
In case that there are differences, you have access to a the comparision table so it would be easier to find the RC. <br> 

Inputs are:
1. `header.txt` : Used for common definitions, such as the time range on which both queries are compared on or for newly defined comparators
2. `columns_comparators.txt` : The comparators to use when comparing the columns, [see explanation below](#comparators) <br>
3. `gold.kql` : Original KQL Query
4. `refactored.kql` : Refactored KQL Query
5. `gold_columns.txt` : The names of the columns of the original query
6. `refactored_columns.txt` : The names of the columns of the refactored query

NOTE! <br>
The first column in each of the files { `gold_columns.txt`, `refactored_columns.txt` } should be a unique identifier that is used to match row-to-row the values of the tables.

e.g. <br>
Matching by ID number.

Output is:
1. `compare.kql` : The KQL query that used to compare both queries

<!-- USAGE INSTRUCTIONS -->
## Usage
The tool generates a query that matches row-by-row the results of the gold and refactored queries results tables.
The comparision of each of the columns is based on the the comparators names file supplied.

### Comparators
The given comparators are:
1. `regulars` : Comparision with the equal operator, `x == y`
2. `reals` : Comparision of float numbers, `double` type, comparision is if difference between absolute value is less than `EPSILON=10e-5`
3. `series` : Comparision of an array of float numbers, by subtraction of the values and checking if both minimum and maximum values of the result are less than `EPSILON=10e-5`

You can extend and implement your own comparator and adding it to the header file, such that it's name follows the following convention `is_[comparators]_equals` <br>

e.g.
Just as a trivial example, case insensitive comparision of strings
```kql
let are_strings_equal = (s1:string, s2:string)
{
    s1 =~ s2
};
```

In particular this can be extended to create a similarity comparator:
```kql
let are_series_sim_equal = (vec1:dynamic, vec2:dynamic)
{
    let SIMILARITY_THRESHOLD = 0.5;
    let subtract = series_subtract(vec1, vec2);
    // If there is a ground truth data array, choose it as the avg
    // Else, when can't estimate which is more accurate choosing average of averages 
    let avg1 = todouble(series_stats_dynamic(vec1)['avg']);
    let avg2 = todouble(series_stats_dynamic(vec2)['avg']);
    let max_diff = abs(todouble(series_stats_dynamic(subtract)['max']));
    let avg_avg = (avg1 + avg2) / 2.0;    
    let similarity = max_diff / avg_avg;
    similarity < SIMILARITY_THRESHOLD
};
```

## Best Practices
1. In case the refactored query is of a new table, where the timestamps are not the same due to sporadic delays, but the data itself should be similar; <br>
1.1. Define in `header.txt` the same time range on which the results are queried, make sure to exclude last bin of data (because it contains only partial results) <br>
1.2. Utilize [`make-series`](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/make-seriesoperator) operator to aggregate the data to bins. This allows to compare aggregated rows instead of single-row to single-row  <br>
1.3. The framework will match row-to-row by the unique-id and compare each of the aggregated columns based on the comparator, you can define similarity comparators  

Header:
```
let start   = ago(1d);
let end     = ago(1h);
let dt      = 1h;
```

Query:
```
Table
| where Timestamp between(start..end)
| make-series AvgData = avg(Data)
              on Timestamp from start to end step dt by UniqueId
| project UniqueId, AvgData
```

<!-- CONTACT -->
## Contact

Aviv Yaniv <br>
[Email](avivyaniv@microsoft.com)
[Site](https://www.tau.ac.il/~avivyaniv/)
[Blog](https://avivyaniv.medium.com/)
[StackOverflow](https://stackoverflow.com/users/14148864/aviv-yaniv?tab=profile)
[GitHub](https://github.com/AvivYaniv)
[Project Euler](https://projecteuler.net/profile/Aviv_Yaniv.png)

<p align="right">(<a href="#top">back to top</a>)</p>
