FuzzyFinder.Net
----

FuzzyFinder allows you to quickly search for and open files with just a few key
presses.

It is mostly intended for programmers, where source trees are well defined and
organised. But anyone who wishes to easily find files in an entire directory
(sub-)tree may find it useful.


Inspiration
---

I had previsouly been using VIM (vim.org) with the fuzzy_finder_textmate script.
VIM is not my favourite text editor for every type of task, but I found the
fuzzy finder so useful that I wanted to create a more general purpose solution.

FuzzyFinder.Net was born!

By creating a fuzzy finder that could be used as a GUI app I can now find files
quickly and open them in any application.


Requirements
---

1. Windows XP/Vista/7
2. Ruby
3. fuzzy_file_finder
4. Ruby Gem: win32-pipe
5. Microsoft .Net


Getting Started
---

There are a couple of step you need to take to get FuzzyFinder up and running.


1. Install Ruby
--

After install installing Ruby with RubyGems, run the following two commands:

gem install win32-pipe
gem install -source=gems.github.com jamis-fuzzy_file_finder 


2. Install FuzzyFinder.Net
--

Compile from source or unzip the package. You should have two files in the same
directory: fuzz.rb and FuzzyFinder.exe.

Double click FuzzyFinder.exe to start.
