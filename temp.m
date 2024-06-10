tester = [0,0,0;3,3,3;2,2,2;4,4,4];
target=[2,3,4];

[c,ia,ib]=intersect(tester(:,1:2),target(1:2),'rows');

if isempty(ia)
    tester = [tester;target];
else
    tester(ia,3)=tester(ia,3)+target(3);
end


tester
