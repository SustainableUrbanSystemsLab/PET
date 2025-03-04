
using math;

using System.Collections.Generic;

using System.Linq;

public static class PET {
    
    public static object physiologicalEquivalentTemperature(
        object Ta,
        object mrt,
        object rh,
        object ws,
        object age,
        object sex,
        object heightM,
        object weight,
        object bodyPosition,
        object Icl,
        object M,
        object climate) {
        // inputs: (climate, Ta, ws, rh, MRT, age, sex, heightM, weight, bodyPosition, M, Icl):
        // based on: Peter Hoeppe PET fortran code, from:
        // "Urban climatic map and standards for wind environment - Feasibility study, Technical Input Report No.1",
        // The Chinese University of Hong Kong, Planning Department, Nov 2008
        var petObj = physiologicalEquivalentTemperatureC(Ta, mrt, rh, ws, age, sex, heightM, weight, bodyPosition, M, Icl);
        var respiration = petObj.inkoerp();
        var _tup_1 = petObj.berech();
        var coreTemperature = _tup_1.Item1;
        var radiationBalance = _tup_1.Item2;
        var convection = _tup_1.Item3;
        var waterVaporDiffusion = _tup_1.Item4;
        petObj.pet();
        var skinTemperature = petObj.tsk;
        var totalHeatLoss = petObj.wsum;
        var skinSweating = petObj.wetsk;
        var internalHeat = petObj.h;
        var sweatEvaporation = petObj.esw;
        var PET = petObj.tx;
        var _tup_2 = petObj.thermalCategories(climate);
        var effectPET = _tup_2.Item1;
        var comfortablePET = _tup_2.Item2;
        var PETresults = new List<object> {
            coreTemperature,
            skinTemperature,
            totalHeatLoss,
            skinSweating,
            internalHeat,
            radiationBalance,
            convection,
            waterVaporDiffusion,
            sweatEvaporation,
            respiration
        };
        return Tuple.Create(PET, effectPET, comfortablePET, PETresults);
    }
    
    public class physiologicalEquivalentTemperatureC {
        
        public physiologicalEquivalentTemperatureC(
            object Ta,
            object MRT,
            object rh,
            object ws,
            object age,
            object sex,
            object heightM,
            object weight,
            object bodyPosition,
            object M,
            object Icl) {
            // personal inputs
            this.ta = Ta;
            this.tmrt = MRT;
            this.rh = rh;
            this.v = ws;
            this.vpa = this.rh / 100.0 * 6.105 * math.exp(17.27 * this.ta / (237.7 + this.ta));
            this.age = age;
            if (sex == "male") {
                sex = 1;
            } else if (sex == "female") {
                sex = 2;
            } else if (sex == "average sex") {
                sex = 3;
            }
            this.sex = sex;
            this.ht = heightM;
            this.mbody = weight;
            this.work = M;
            if (Icl - 0.02 < 0.01) {
                Icl = 0.02;
            }
            this.icl = Icl;
            this.eta = 0.0;
            this.fcl = 1 + 0.31 * this.icl;
            if (bodyPosition == "sitting") {
                this.feff = 0.696;
            } else if (bodyPosition == "standing") {
                this.feff = 0.725;
            } else if (bodyPosition == "crouching") {
                this.feff = 0.67;
            }
            // constants
            this.po = 1013.25;
            this.p = 1013.25;
            this.rob = 1.06;
            this.cb = 3640.0;
            this.food = 0.0;
            this.emsk = 0.99;
            this.emcl = 0.95;
            this.evap = 2.42 * math.pow(10.0, 6.0);
            this.sigm = 5.67 * math.pow(10.0, -8.0);
        }
        
        public virtual object inkoerp() {
            // inner body energy
            var eswdif = 3.19 * math.pow(this.mbody, 0.75) * (1.0 + 0.004 * (30.0 - this.age) + 0.018 * (this.ht * 100.0 / math.pow(this.mbody, 1.0 / 3.0) - 42.1));
            var eswphy = 3.45 * math.pow(this.mbody, 0.75) * (1.0 + 0.004 * (30.0 - this.age) + 0.01 * (this.ht * 100.0 / math.pow(this.mbody, 1.0 / 3.0) - 43.4));
            var eswpot = this.work + eswphy;
            var fec = this.work + eswdif;
            var he = 0.0;
            if (this.sex == 1) {
                he = eswpot;
            } else if (this.sex == 2) {
                he = fec;
            } else if (this.sex == 3) {
                he = (eswpot + fec) / 2;
            }
            this.h = he * (1.0 - this.eta);
            // sensible respiratory energy
            this.cair = 1010.0;
            this.tex = 0.47 * this.ta + 21.0;
            this.rtv = 1.44 * math.pow(10.0, -6.0) * he;
            this.eres = this.cair * (this.ta - this.tex) * this.rtv;
            // deferred respiration energy
            this.vpex = 6.11 * math.pow(10.0, 7.45 * this.tex / (235.0 + this.tex));
            this.erel = 0.623 * this.evap / this.p * (this.vpa - this.vpex) * this.rtv;
            this.ere = this.eres + this.erel;
            return this.ere;
        }
        
        public virtual object berech() {
            object index;
            object wr;
            object wd;
            object ws;
            object g100;
            object vb;
            object sw;
            object y;
            this.wetsk = 0.0;
            var c = (from i in range(11)
                select null).ToList();
            var tcore = (from i in range(7)
                select null).ToList();
            this.adu = 0.203 * math.pow(this.mbody, 0.425) * math.pow(this.ht, 0.725);
            this.hc = 2.67 + 6.5 * math.pow(this.v, 0.67);
            this.hc = this.hc * math.pow(this.p / this.po, 0.55);
            this.facl = (173.51 * this.icl - 2.36 - 100.76 * this.icl * this.icl + 19.28 * math.pow(this.icl, 3.0)) / 100.0;
            if (this.facl > 1.0) {
                this.facl = 1.0;
            }
            var rcl = this.icl / 6.45 / this.facl;
            if (this.icl >= 2.0) {
                y = 1.0;
            }
            if (this.icl > 0.6 && this.icl < 2.0) {
                y = (this.ht - 0.2) / this.ht;
            }
            if (this.icl <= 0.6 && this.icl > 0.3) {
                y = 0.5;
            }
            if (this.icl <= 0.3 && this.icl > 0.0) {
                y = 0.1;
            }
            var r2 = this.adu * (this.fcl - 1.0 + this.facl) / (6.28 * this.ht * y);
            var r1 = this.facl * this.adu / (6.28 * this.ht * y);
            var di = r2 - r1;
            // skin temperatures
            foreach (var j in range(1, 7)) {
                this.tsk = 34.0;
                this.count1 = 0;
                this.tcl = (this.ta + this.tmrt + this.tsk) / 3.0;
                this.enbal2 = 0.0;
                while (true) {
                    foreach (var count2 in range(1, 100)) {
                        this.acl = this.adu * this.facl + this.adu * (this.fcl - 1.0);
                        var rclo2 = this.emcl * this.sigm * (math.pow(this.tcl + 273.2, 4.0) - math.pow(this.tmrt + 273.2, 4.0)) * this.feff;
                        var htcl = 6.28 * this.ht * y * di / (rcl * math.log(r2 / r1) * this.acl);
                        this.tsk = 1.0 / htcl * (this.hc * (this.tcl - this.ta) + rclo2) + this.tcl;
                        // radiation balance
                        this.aeff = this.adu * this.feff;
                        this.rbare = this.aeff * (1.0 - this.facl) * this.emsk * this.sigm * (math.pow(this.tmrt + 273.2, 4.0) - math.pow(this.tsk + 273.2, 4.0));
                        this.rclo = this.feff * this.acl * this.emcl * this.sigm * (math.pow(this.tmrt + 273.2, 4.0) - math.pow(this.tcl + 273.2, 4.0));
                        this.rsum = this.rbare + this.rclo;
                        // convection
                        this.cbare = this.hc * (this.ta - this.tsk) * this.adu * (1.0 - this.facl);
                        this.cclo = this.hc * (this.ta - this.tcl) * this.acl;
                        this.csum = this.cbare + this.cclo;
                        // core temperature
                        c[0] = this.h + this.ere;
                        c[1] = this.adu * this.rob * this.cb;
                        c[2] = 18.0 - 0.5 * this.tsk;
                        c[3] = 5.28 * this.adu * c[2];
                        c[4] = 13.0 / 625.0 * c[1];
                        c[5] = 0.76075 * c[1];
                        c[6] = c[3] - c[5] - this.tsk * c[4];
                        c[7] = -c[0] * c[2] - this.tsk * c[3] + this.tsk * c[5];
                        c[8] = c[6] * c[6] - 4.0 * c[4] * c[7];
                        c[9] = 5.28 * this.adu - c[5] - c[4] * this.tsk;
                        c[10] = c[9] * c[9] - 4.0 * c[4] * (c[5] * this.tsk - c[0] - 5.28 * this.adu * this.tsk);
                        if (this.tsk == 36.0) {
                            this.tsk = 36.01;
                        }
                        tcore[6] = c[0] / (5.28 * this.adu + c[1] * 6.3 / 3600.0) + this.tsk;
                        tcore[2] = c[0] / (5.28 * this.adu + c[1] * 6.3 / 3600.0 / (1.0 + 0.5 * (34.0 - this.tsk))) + this.tsk;
                        if (c[10] >= 0.0) {
                            tcore[5] = (-c[9] - math.pow(c[10], 0.5)) / (2.0 * c[4]);
                            tcore[0] = (-c[9] + math.pow(c[10], 0.5)) / (2.0 * c[4]);
                        }
                        if (c[8] >= 0.0) {
                            tcore[1] = (-c[6] + math.pow(abs(c[8]), 0.5)) / (2.0 * c[4]);
                            tcore[4] = (-c[6] - math.pow(abs(c[8]), 0.5)) / (2.0 * c[4]);
                        }
                        tcore[3] = c[0] / (5.28 * this.adu + c[1] * 1.0 / 40.0) + this.tsk;
                        // transpiration
                        var tbody = 0.1 * this.tsk + 0.9 * tcore[j - 1];
                        var swm = 304.94 * (tbody - 36.6) * this.adu / 3600000.0;
                        this.vpts = 6.11 * math.pow(10.0, 7.45 * this.tsk / (235.0 + this.tsk));
                        if (tbody <= 36.6) {
                            swm = 0.0;
                        }
                        var swf = 0.7 * swm;
                        if (this.sex == 1) {
                            sw = swm;
                        }
                        if (this.sex == 2) {
                            sw = swf;
                        }
                        if (this.sex == 3) {
                            sw = (swm + swf) / 2;
                        }
                        var eswphy = -sw * this.evap;
                        var he = 0.633 * this.hc / (this.p * this.cair);
                        var fec = 1.0 / (1.0 + 0.92 * this.hc * rcl);
                        var eswpot = he * (this.vpa - this.vpts) * this.adu * this.evap * fec;
                        this.wetsk = eswphy / eswpot;
                        if (this.wetsk > 1.0) {
                            this.wetsk = 1.0;
                        }
                        var eswdif = eswphy - eswpot;
                        if (eswdif <= 0.0) {
                            this.esw = eswpot;
                        }
                        if (eswdif > 0.0) {
                            this.esw = eswphy;
                        }
                        if (this.esw > 0.0) {
                            this.esw = 0.0;
                        }
                        // diffusion
                        this.rdsk = 0.79 * math.pow(10.0, 7.0);
                        this.rdcl = 0.0;
                        this.ed = this.evap / (this.rdsk + this.rdcl) * this.adu * (1.0 - this.wetsk) * (this.vpa - this.vpts);
                        // max vb
                        var vb1 = 34.0 - this.tsk;
                        var vb2 = tcore[j - 1] - 36.6;
                        if (vb2 < 0.0) {
                            vb2 = 0.0;
                        }
                        if (vb1 < 0.0) {
                            vb1 = 0.0;
                        }
                        vb = (6.3 + 75.0 * vb2) / (1.0 + 0.5 * vb1);
                        // energy balance
                        this.enbal = this.h + this.ed + this.ere + this.esw + this.csum + this.rsum + this.food;
                        // clothing temperature
                        if (this.count1 == 0) {
                            this.xx = 1.0;
                        }
                        if (this.count1 == 1) {
                            this.xx = 0.1;
                        }
                        if (this.count1 == 2) {
                            this.xx = 0.01;
                        }
                        if (this.count1 == 3) {
                            this.xx = 0.001;
                        }
                        if (this.enbal > 0.0) {
                            this.tcl = this.tcl + this.xx;
                        }
                        if (this.enbal < 0.0) {
                            this.tcl = this.tcl - this.xx;
                        }
                        if ((this.enbal > 0.0 || this.enbal2 <= 0.0) && (this.enbal < 0.0 || this.enbal2 >= 0.0)) {
                            this.enbal2 = this.enbal;
                            count2 += 1;
                        } else {
                            break;
                        }
                    }
                    if (this.count1 == 0.0 || this.count1 == 1.0 || this.count1 == 2.0) {
                        this.count1 = this.count1 + 1;
                        this.enbal2 = 0.0;
                    } else {
                        break;
                    }
                }
                foreach (var k in range(20)) {
                    if (this.count1 == 3.0 && (j != 2 && j != 5)) {
                        if (j != 6 && j != 1) {
                            if (j != 3) {
                                if (j != 7) {
                                    if (j == 4) {
                                        g100 = true;
                                        break;
                                    }
                                } else {
                                    if (tcore[j - 1] >= 36.6 || this.tsk <= 34.0) {
                                        g100 = false;
                                        break;
                                    }
                                    g100 = true;
                                    break;
                                }
                            } else {
                                if (tcore[j - 1] >= 36.6 || this.tsk > 34.0) {
                                    g100 = false;
                                    break;
                                }
                                g100 = true;
                                break;
                            }
                        } else {
                            if (c[10] < 0.0 || (tcore[j - 1] < 36.6 || this.tsk <= 33.85)) {
                                g100 = false;
                                break;
                            }
                            g100 = true;
                            break;
                        }
                    }
                    if (c[8] < 0.0 || (tcore[j - 1] < 36.6 || this.tsk > 34.05)) {
                        g100 = false;
                        break;
                    }
                }
                if (g100 == false) {
                    continue;
                } else if ((j == 4 || vb < 91.0) && (j != 4 || vb >= 89.0)) {
                    if (vb > 90.0) {
                        vb = 90.0;
                    }
                    // water loss
                    ws = sw * 3600.0 * 1000.0;
                    if (ws > 2000.0) {
                        ws = 2000.0;
                    }
                    wd = this.ed / this.evap * 3600.0 * -1000.0;
                    wr = this.erel / this.evap * 3600.0 * -1000.0;
                    this.wsum = ws + wr + wd;
                    return Tuple.Create(tcore[j - 1], this.rsum, this.csum, this.ed);
                }
                // water loss
                ws = sw * 3600.0 * 1000.0;
                wd = this.ed / this.evap * 3600.0 * -1000.0;
                wr = this.erel / this.evap * 3600.0 * -1000.0;
                this.wsum = ws + wr + wd;
                if (j - 3 < 0) {
                    index = 3;
                } else {
                    index = j - 3;
                }
                return Tuple.Create(tcore[index], this.rsum, this.csum, this.ed);
            }
        }
        
        public virtual object pet() {
            this.tx = this.ta;
            this.enbal2 = 0.0;
            this.count1 = 0;
            while (this.count1 != 4) {
                this.hc = 2.67 + 6.5 * math.pow(0.1, 0.67);
                this.hc = this.hc * math.pow(this.p / this.po, 0.55);
                // radiation saldo
                this.aeff = this.adu * this.feff;
                this.rbare = this.aeff * (1.0 - this.facl) * this.emsk * this.sigm * (math.pow(this.tx + 273.2, 4.0) - math.pow(this.tsk + 273.2, 4.0));
                this.rclo = this.feff * this.acl * this.emcl * this.sigm * (math.pow(this.tx + 273.2, 4.0) - math.pow(this.tcl + 273.2, 4.0));
                this.rsum = this.rbare + this.rclo;
                // convection
                this.cbare = this.hc * (this.tx - this.tsk) * this.adu * (1.0 - this.facl);
                this.cclo = this.hc * (this.tx - this.tcl) * this.acl;
                this.csum = this.cbare + this.cclo;
                // diffusion
                this.ed = this.evap / (this.rdsk + this.rdcl) * this.adu * (1.0 - this.wetsk) * (12.0 - this.vpts);
                // breathing
                this.tex = 0.47 * this.tx + 21.0;
                this.eres = this.cair * (this.tx - this.tex) * this.rtv;
                this.vpex = 6.11 * math.pow(10.0, 7.45 * this.tex / (235.0 + this.tex));
                this.erel = 0.623 * this.evap / this.p * (12.0 - this.vpex) * this.rtv;
                this.ere = this.eres + this.erel;
                // energy balance
                this.enbal = this.h + this.ed + this.ere + this.esw + this.csum + this.rsum;
                if (this.count1 == 0) {
                    this.xx = 1.0;
                }
                if (this.count1 == 1) {
                    this.xx = 0.1;
                }
                if (this.count1 == 2) {
                    this.xx = 0.01;
                }
                if (this.count1 == 3) {
                    this.xx = 0.001;
                }
                if (this.enbal > 0.0) {
                    this.tx = this.tx - this.xx;
                }
                if (this.enbal < 0.0) {
                    this.tx = this.tx + this.xx;
                }
                if ((this.enbal > 0.0 || this.enbal2 <= 0.0) && (this.enbal < 0.0 || this.enbal2 >= 0.0)) {
                    this.enbal2 = this.enbal;
                } else {
                    this.count1 = this.count1 + 1;
                }
            }
            return;
        }
        
        public virtual object thermalCategories(object climate) {
            object comfortablePET;
            object effectPET;
            var PET = this.tx;
            if (climate == "humid") {
                // categories by Lin and Matzarakis (2008) (tropical and subtropical humid climate)
                if (PET < 14) {
                    effectPET = -4;
                    comfortablePET = 0;
                } else if (PET >= 14 && PET < 18) {
                    effectPET = -3;
                    comfortablePET = 0;
                } else if (PET >= 18 && PET < 22) {
                    effectPET = -2;
                    comfortablePET = 0;
                } else if (PET >= 22 && PET < 26) {
                    effectPET = -1;
                    comfortablePET = 0;
                } else if (PET >= 26 && PET <= 30) {
                    effectPET = 0;
                    comfortablePET = 1;
                } else if (PET > 30 && PET <= 34) {
                    effectPET = 1;
                    comfortablePET = 0;
                } else if (PET > 34 && PET <= 38) {
                    effectPET = 2;
                    comfortablePET = 0;
                } else if (PET > 38 && PET <= 42) {
                    effectPET = 3;
                    comfortablePET = 0;
                } else if (PET > 42) {
                    effectPET = 4;
                    comfortablePET = 0;
                }
            } else if (climate == "temperate") {
                // categories by Matzarakis and Mayer (1996) (temperate climate)
                if (PET < 4) {
                    effectPET = -4;
                    comfortablePET = 0;
                } else if (PET >= 4 && PET < 8) {
                    effectPET = -3;
                    comfortablePET = 0;
                } else if (PET >= 8 && PET < 13) {
                    effectPET = -2;
                    comfortablePET = 0;
                } else if (PET >= 13 && PET < 18) {
                    effectPET = -1;
                    comfortablePET = 0;
                } else if (PET >= 18 && PET <= 23) {
                    effectPET = 0;
                    comfortablePET = 1;
                } else if (PET > 23 && PET <= 29) {
                    effectPET = 1;
                    comfortablePET = 0;
                } else if (PET > 29 && PET <= 35) {
                    effectPET = 2;
                    comfortablePET = 0;
                } else if (PET > 35 && PET <= 41) {
                    effectPET = 3;
                    comfortablePET = 0;
                } else if (PET > 41) {
                    effectPET = 4;
                    comfortablePET = 0;
                }
            }
            return Tuple.Create(effectPET, comfortablePET);
        }
    }
    
    public static object a = physiologicalEquivalentTemperature(20, 20, 20, 20, 2, m, 1, 70, "standing");
}
