# Polls the GH checks API for a check specified via envvars.
# Needs: CHECK, TOKEN and SHA envvars.

$url = "$($env:GITHUB_API_URL)/repos/$($env:GITHUB_REPOSITORY)/commits/$($env:SHA)/check-runs?check_name=$($env:CHECK)"
# don't sleep on first loop
$sleep = 0;
do {
    write-host "Checking $url ..."
    sleep -s $sleep
    # by writing the body of the result to check.json we can get just the status code from -w via curl
    $result = curl -s -w "%{response_code}" $url -H "Authorization: token $($env:TOKEN)" -o check.json
    if ($result -eq "200") {
        # only if we succeed in the HTTP get, we know we can parse from JSON the check API result
        $status = get-content check.json | convertfrom-json | select -ExpandProperty check_runs | select -ExpandProperty conclusion
        write-host "Check status is $status"
    } else {
        write-host "Request status was $result..."
    }
    # sleep 5' from now on
    $sleep = 5
} while ($result -ne "200" -or $null -eq $status)

if ($status -ne "success") { 
    throw "Status check '$($env:CHECK)' was '$status' instead of expected 'success'"
}