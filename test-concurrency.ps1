$slug = "joes-barbershop"
$apiUrl = "http://localhost:5088/api/businesses/$slug"

Write-Host "Fetching business details to get a valid ServiceTypeId..."
$business = Invoke-RestMethod -Uri $apiUrl -Method Get
$serviceTypeId = $business.serviceTypes[0].id

Write-Host "Joining the queue twice..."
$ticket1 = Invoke-RestMethod -Uri "$apiUrl/queue" -Method Post -ContentType "application/json" -Body "{`"serviceTypeId`": `"$serviceTypeId`"}"
$ticket2 = Invoke-RestMethod -Uri "$apiUrl/queue" -Method Post -ContentType "application/json" -Body "{`"serviceTypeId`": `"$serviceTypeId`"}"

Write-Host "Ticket 1: $($ticket1.id)"
Write-Host "Ticket 2: $($ticket2.id)"

Write-Host "Simulating two concurrent 'Call Next' requests..."

# Start two concurrent jobs
$job1 = Start-Job -ScriptBlock {
    param($url)
    $response = Invoke-RestMethod -Uri "$url/queue/call-next" -Method Post
    return "Job 1 called ticket: $($response.id)"
} -ArgumentList $apiUrl

$job2 = Start-Job -ScriptBlock {
    param($url)
    $response = Invoke-RestMethod -Uri "$url/queue/call-next" -Method Post
    return "Job 2 called ticket: $($response.id)"
} -ArgumentList $apiUrl

# Wait for both to finish
Wait-Job $job1, $job2 | Out-Null

# Get results
Receive-Job $job1
Receive-Job $job2

# Clean up
Remove-Job $job1, $job2

Write-Host "Concurrency test complete."
