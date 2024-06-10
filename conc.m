function [conc] = conc(x,y,z, emissionMassFlowRate, windVelocity, dispersionCoefficientZ, dispersionCoefficientY, lengthwiseResolution, a, steadyStateHeight)

term1=emissionMassFlowRate/(2*pi()*windVelocity*dispersionCoefficientZ(ceil(x/lengthwiseResolution))*dispersionCoefficientY(ceil(x/lengthwiseResolution)));
term2=exp((-((y)^2))/(2*dispersionCoefficientY(ceil(x/lengthwiseResolution)).^2));
term3=exp((-((z-steadyStateHeight)^2))/(2*dispersionCoefficientZ(ceil(x/lengthwiseResolution)).^2));
term4=a*exp((-((z+steadyStateHeight)^2))/(2*dispersionCoefficientZ(ceil(x/lengthwiseResolution)).^2));

conc = term1*term2*(term3+term4);

end

