function point_current = findMax(U,Thr,Q,dispY,dispZ,steadyStateHeight,XorYorZ)

point_current = 0;
point = -1;
x = 0;
step = 30;
curconc = 2 * Thr;

if XorYorZ == 'x'
    while curconc>Thr
        point_current=point_current+step;
        dispCoeffY = dispY(1)*point_current*(1+dispY(2)*point_current)^dispY(3);
        dispCoeffZ = dispZ(1)*point_current*(1+dispZ(2)*point_current)^dispZ(3);
        curconc=(1+exp(-4*steadyStateHeight^2/(2*dispCoeffZ^2)))*Q/(2*pi()*U*dispCoeffZ*dispCoeffY);
    end
else
    if XorYorZ == 'y'
        choice=1;
    elseif XorYorZ == 'z'
        choice=0;
    end
    while point_current>point
        x=x+step;
        point=point_current;
        dispCoeffY = dispY(1)*x*(1+dispY(2)*x)^dispY(3);
        dispCoeffZ = dispZ(1)*x*(1+dispZ(2)*x)^dispZ(3);
        point_current=sqrt(-log(2*pi()*U*Thr*dispCoeffZ*dispCoeffY/Q)*2)*((dispCoeffY^choice)*dispCoeffZ^(1-choice));
    end
end
end