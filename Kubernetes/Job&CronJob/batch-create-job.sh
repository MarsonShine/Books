mkdir ./jobs
for i in apple banana cherry
do
    cat batch-job-template.yaml | sed "s/\$ITEM/$i/" > ./jobs/job-$i.yaml
done