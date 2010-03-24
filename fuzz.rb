require 'win32/pipe'
include Win32
require 'fuzzy_file_finder'

def debug(s)
#	puts s
end

def find_matches(name)
	matchesStr = ""
	matches = []

	return "|Too many entries\n" if FINDER == false

	matches = FINDER.find(name,20).sort_by do |m|
		[m[:score], m[:path]]
	end

	matchesStr = "\n"

	matches.reverse!
	matches.each_with_index do |match, index|
		debug "%s\n" % [match[:highlighted_path]]
		matchesStr << "%s|%s\n" % [match[:path], match[:highlighted_path]]
	end
	matchesStr
end

# Block form
Pipe::Client.new('fuzzy') do |pipe|
	begin
		FINDER = FuzzyFileFinder.new
	rescue
		FINDER = false
	end
	
	debug "Connected..."
	while true
		data = pipe.read.first.strip
		debug "Using [#{data}] as lookup"
		pipe.write(find_matches(data))
	end
end
