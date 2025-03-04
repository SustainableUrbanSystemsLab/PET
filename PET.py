
import math





def physiologicalEquivalentTemperature(Ta, mrt,  rh, ws,   age, sex,  heightM, weight, bodyPosition, Icl,  M,  climate):
    # ICl: total thermal resistance
    # inputs: (climate, Ta, ws, rh, MRT, age, sex, heightM, weight, bodyPosition, M, Icl):
    # based on: Peter Hoeppe PET fortran code, from:
    # "Urban climatic map and standards for wind environment - Feasibility study, Technical Input Report No.1",
    # The Chinese University of Hong Kong, Planning Department, Nov 2008

    petObj = physiologicalEquivalentTemperatureC(Ta, mrt, rh, ws, age, sex, heightM, weight, bodyPosition, M, Icl)
    respiration = petObj.inkoerp()
    coreTemperature, radiationBalance, convection, waterVaporDiffusion = petObj.berech()
    petObj.pet()
    skinTemperature, totalHeatLoss, skinSweating, internalHeat, sweatEvaporation, PET = petObj.tsk, petObj.wsum, petObj.wetsk, petObj.h, petObj.esw, petObj.tx
    effectPET, comfortablePET = petObj.thermalCategories(climate)

    PETresults = [coreTemperature, skinTemperature, totalHeatLoss, skinSweating, internalHeat, radiationBalance, convection, waterVaporDiffusion, sweatEvaporation, respiration]

    return PET, effectPET, comfortablePET, PETresults




#(self, Ta, MRT, rh, ws, age, sex, heightM, weight, bodyPosition, M, Icl):):










class physiologicalEquivalentTemperatureC():
      # based on: Peter Hoeppe PET fortran code, from:
      # "Urban climatic map and standards for wind environment - Feasibility study, Technical Input Report No.1",
      # The Chinese University of Hong Kong, Planning Department, Nov 2008

      def __init__(self, Ta, MRT, rh, ws, age, sex, heightM, weight, bodyPosition, M, Icl):
          # personal inputs
          self.ta = Ta  # in Celsius degrees
          self.tmrt = MRT  # in Celsius degrees
          self.rh = rh  # in %
          self.v = ws  # in m/s
          self.vpa = self.rh / 100.0 * 6.105 * math.exp(17.27 * self.ta / (237.7 + self.ta))
          self.age = age  # in years
          if sex == "male": sex = 1
          elif sex == "female": sex = 2
          elif sex == "average sex": sex = 3
          self.sex = sex
          self.ht = heightM  # in meters
          self.mbody = weight  # in kg
          self.work = M  # in W/m2
          if ((Icl-0.02) < 0.01):
              Icl = 0.02
          self.icl = Icl
          self.eta = 0.0
          self.fcl = 1 + (0.31*self.icl)
          if bodyPosition == "sitting":
              self.feff = 0.696
          elif bodyPosition == "standing":
              self.feff = 0.725
          elif bodyPosition == "crouching":
              self.feff = 0.67
          # constants
          self.po = 1013.25
          self.p = 1013.25
          self.rob = 1.06
          self.cb = 3640.0
          self.food = 0.0
          self.emsk = 0.99
          self.emcl = 0.95
          self.evap = 2.42 * math.pow(10.0, 6.0)
          self.sigm = 5.67 * math.pow(10.0, -8.0)

      def inkoerp(self):
          # inner body energy
          eswdif = 3.19 * math.pow(self.mbody, 0.75) * (1.0 + 0.004 * (30.0 - self.age) + 0.018 * (self.ht * 100.0 / math.pow(self.mbody, 1.0 / 3.0) - 42.1))
          eswphy = 3.45 * math.pow(self.mbody, 0.75) * (1.0 + 0.004 * (30.0 - self.age) + 0.01 * (self.ht * 100.0 / math.pow(self.mbody, 1.0 / 3.0) - 43.4))
          eswpot = self.work + eswphy
          fec = self.work + eswdif
          he = 0.0
          if self.sex == 1:
              he = eswpot
          elif self.sex == 2:
              he = fec
          elif self.sex == 3:
              he = (eswpot + fec)/2
          self.h = he * (1.0 - self.eta)
          # sensible respiratory energy
          self.cair = 1010.0
          self.tex = 0.47 * self.ta + 21.0
          self.rtv = 1.44 * math.pow(10.0, -6.0) * he
          self.eres = self.cair * (self.ta - self.tex) * self.rtv
          # deferred respiration energy
          self.vpex = 6.11 * math.pow(10.0, 7.45 * self.tex / (235.0 + self.tex))
          self.erel = 0.623 * self.evap / self.p * (self.vpa - self.vpex) * self.rtv
          self.ere = self.eres + self.erel

          return self.ere

      def berech(self):
          self.wetsk = 0.0
          c = [None for i in range(11)]
          tcore = [None for i in range(7)]
          self.adu = 0.203 * math.pow(self.mbody, 0.425) * math.pow(self.ht, 0.725)
          self.hc = 2.67 + 6.5 * math.pow(self.v, 0.67)
          self.hc = self.hc * math.pow(self.p / self.po, 0.55)
          self.facl = (173.51 * self.icl - 2.36 - 100.76 * self.icl * self.icl + 19.28 * math.pow(self.icl, 3.0)) / 100.0
          if self.facl > 1.0:
              self.facl = 1.0
          rcl = self.icl / 6.45 / self.facl
          if self.icl >= 2.0:
              y = 1.0
          if self.icl > 0.6 and self.icl < 2.0:
              y = (self.ht - 0.2) / self.ht
          if self.icl <= 0.6 and self.icl > 0.3:
              y = 0.5
          if self.icl <= 0.3 and self.icl > 0.0:
              y = 0.1
          r2 = self.adu * (self.fcl - 1.0 + self.facl) / (6.28 * self.ht * y)
          r1 = self.facl * self.adu / (6.28 * self.ht * y)
          di = r2 - r1
          # skin temperatures
          for j in range(1,7):
              self.tsk = 34.0
              self.count1 = 0
              self.tcl = (self.ta + self.tmrt + self.tsk) / 3.0
              self.enbal2 = 0.0
              while True:
                  for count2 in range(1,100):
                      self.acl = self.adu * self.facl + self.adu * (self.fcl - 1.0)
                      rclo2 = self.emcl * self.sigm * (math.pow(self.tcl + 273.2, 4.0) - math.pow(self.tmrt + 273.2, 4.0)) * self.feff
                      htcl = 6.28 * self.ht * y * di / (rcl * math.log(r2 / r1) * self.acl)
                      self.tsk = 1.0 / htcl * (self.hc * (self.tcl - self.ta) + rclo2) + self.tcl
                      # radiation balance
                      self.aeff = self.adu * self.feff
                      self.rbare = self.aeff * (1.0 - self.facl) * self.emsk * self.sigm * (math.pow(self.tmrt + 273.2, 4.0) - math.pow(self.tsk + 273.2, 4.0))
                      self.rclo = self.feff * self.acl * self.emcl * self.sigm * (math.pow(self.tmrt + 273.2, 4.0) - math.pow(self.tcl + 273.2, 4.0))
                      self.rsum = self.rbare + self.rclo
                      # convection
                      self.cbare = self.hc * (self.ta - self.tsk) * self.adu * (1.0 - self.facl)
                      self.cclo = self.hc * (self.ta - self.tcl) * self.acl
                      self.csum = self.cbare + self.cclo
                      # core temperature
                      c[0] = self.h + self.ere
                      c[1] = self.adu * self.rob * self.cb
                      c[2] = 18.0 - 0.5 * self.tsk
                      c[3] = 5.28 * self.adu * c[2]
                      c[4] = 13.0 / 625.0 * c[1]
                      c[5] = 0.76075 * c[1]
                      c[6] = c[3] - c[5] - self.tsk * c[4]
                      c[7] = -c[0] * c[2] - self.tsk * c[3] + self.tsk * c[5]
                      c[8] = c[6] * c[6] - 4.0 * c[4] * c[7]
                      c[9] = 5.28 * self.adu - c[5] - c[4] * self.tsk
                      c[10] = c[9] * c[9] - 4.0 * c[4] * (c[5] * self.tsk - c[0] - 5.28 * self.adu * self.tsk)
                      if self.tsk == 36.0:
                          self.tsk = 36.01
                      tcore[6] = c[0] / (5.28 * self.adu + c[1] * 6.3 / 3600.0) + self.tsk
                      tcore[2] = c[0] / (5.28 * self.adu + c[1] * 6.3 / 3600.0 / (1.0 + 0.5 * (34.0 - self.tsk))) + self.tsk
                      if c[10] >= 0.0:
                          tcore[5] = (-c[9] - math.pow(c[10], 0.5)) / (2.0 * c[4])
                          tcore[0] = (-c[9] + math.pow(c[10], 0.5)) / (2.0 * c[4])
                      if c[8] >= 0.0:
                          tcore[1] = (-c[6] + math.pow(abs(c[8]), 0.5)) / (2.0 * c[4])
                          tcore[4] = (-c[6] - math.pow(abs(c[8]), 0.5)) / (2.0 * c[4])
                      tcore[3] = c[0] / (5.28 * self.adu + c[1] * 1.0 / 40.0) + self.tsk
                      # transpiration
                      tbody = 0.1 * self.tsk + 0.9 * tcore[j - 1]
                      swm = 304.94 * (tbody - 36.6) * self.adu / 3600000.0
                      self.vpts = 6.11 * math.pow(10.0, 7.45 * self.tsk / (235.0 + self.tsk))
                      if tbody <= 36.6:
                          swm = 0.0
                      swf = 0.7 * swm
                      if self.sex == 1:
                          sw = swm
                      if self.sex == 2:
                          sw = swf
                      if self.sex == 3:
                          sw = (swm + swf)/2
                      eswphy = -sw * self.evap
                      he = 0.633 * self.hc / (self.p * self.cair)
                      fec = 1.0 / (1.0 + 0.92 * self.hc * rcl)
                      eswpot = he * (self.vpa - self.vpts) * self.adu * self.evap * fec
                      self.wetsk = eswphy / eswpot
                      if self.wetsk > 1.0:
                          self.wetsk = 1.0
                      eswdif = eswphy - eswpot
                      if eswdif <= 0.0:
                          self.esw = eswpot
                      if eswdif > 0.0:
                          self.esw = eswphy
                      if self.esw > 0.0:
                          self.esw = 0.0
                      # diffusion
                      self.rdsk = 0.79 * math.pow(10.0, 7.0)
                      self.rdcl = 0.0
                      self.ed = self.evap / (self.rdsk + self.rdcl) * self.adu * (1.0 - self.wetsk) * (self.vpa - self.vpts)
                      # max vb
                      vb1 = 34.0 - self.tsk
                      vb2 = tcore[j - 1] - 36.6
                      if vb2 < 0.0:
                          vb2 = 0.0
                      if vb1 < 0.0:
                          vb1 = 0.0
                      vb = (6.3 + 75.0 * vb2) / (1.0 + 0.5 * vb1)
                      # energy balance
                      self.enbal = self.h + self.ed + self.ere + self.esw + self.csum + self.rsum + self.food
                      # clothing temperature
                      if self.count1 == 0:
                          self.xx = 1.0
                      if self.count1 == 1:
                          self.xx = 0.1
                      if self.count1 == 2:
                          self.xx = 0.01
                      if self.count1 == 3:
                          self.xx = 0.001
                      if self.enbal > 0.0:
                          self.tcl = self.tcl + self.xx
                      if self.enbal < 0.0:
                          self.tcl = self.tcl - self.xx
                      if (self.enbal > 0.0 or self.enbal2 <= 0.0) and (self.enbal < 0.0 or self.enbal2 >= 0.0):
                          self.enbal2 = self.enbal
                          count2 += 1
                      else:
                          break
                  if self.count1 == 0.0 or self.count1 == 1.0 or self.count1 == 2.0:
                      self.count1 = self.count1 + 1
                      self.enbal2 = 0.0
                  else:
                      break
              for k in range(20):
                  if self.count1 == 3.0 and (j != 2 and j != 5):
                      if j != 6 and j != 1:
                          if j != 3:
                              if j != 7:
                                  if j == 4:
                                      g100 = True
                                      break
                              else:
                                  if tcore[j - 1] >= 36.6 or self.tsk <= 34.0:
                                      g100 = False
                                      break
                                  g100 = True
                                  break
                          else:
                              if tcore[j - 1] >= 36.6 or self.tsk > 34.0:
                                  g100 = False
                                  break
                              g100 = True
                              break
                      else:
                          if c[10] < 0.0 or (tcore[j - 1] < 36.6 or self.tsk <= 33.85):
                              g100 = False
                              break
                          g100 = True
                          break
                  if c[8] < 0.0 or (tcore[j - 1] < 36.6 or self.tsk > 34.05):
                      g100 = False
                      break
              if g100 == False:
                  continue
              else:
                  if (j == 4 or vb < 91.0) and (j != 4 or vb >= 89.0):
                      if vb > 90.0:
                          vb = 90.0
                      # water loss
                      ws = sw * 3600.0 * 1000.0
                      if ws > 2000.0:
                          ws = 2000.0
                      wd = self.ed / self.evap * 3600.0 * (-1000.0)
                      wr = self.erel / self.evap * 3600.0 * (-1000.0)
                      self.wsum = ws + wr + wd
                      return tcore[j-1], self.rsum, self.csum, self.ed
              # water loss
              ws = sw * 3600.0 * 1000.0
              wd = self.ed / self.evap * 3600.0 * (-1000.0)
              wr = self.erel / self.evap * 3600.0 * (-1000.0)
              self.wsum = ws + wr + wd
              if j-3 < 0:
                  index = 3
              else:
                  index = j-3

              return tcore[index], self.rsum, self.csum, self.ed

      def pet(self):
          self.tx = self.ta
          self.enbal2 = 0.0
          self.count1 = 0
          while self.count1 != 4:
              self.hc = 2.67 + 6.5 * math.pow(0.1, 0.67)
              self.hc = self.hc * math.pow(self.p / self.po, 0.55)
              # radiation saldo
              self.aeff = self.adu * self.feff
              self.rbare = self.aeff * (1.0 - self.facl) * self.emsk * self.sigm * (math.pow(self.tx + 273.2, 4.0) - math.pow(self.tsk + 273.2, 4.0))
              self.rclo = self.feff * self.acl * self.emcl * self.sigm * (math.pow(self.tx + 273.2, 4.0) - math.pow(self.tcl + 273.2, 4.0))
              self.rsum = self.rbare + self.rclo
              # convection
              self.cbare = self.hc * (self.tx - self.tsk) * self.adu * (1.0 - self.facl)
              self.cclo = self.hc * (self.tx - self.tcl) * self.acl
              self.csum = self.cbare + self.cclo
              # diffusion
              self.ed = self.evap / (self.rdsk + self.rdcl) * self.adu * (1.0 - self.wetsk) * (12.0 - self.vpts)
              # breathing
              self.tex = 0.47 * self.tx + 21.0
              self.eres = self.cair * (self.tx - self.tex) * self.rtv
              self.vpex = 6.11 * math.pow(10.0, 7.45 * self.tex / (235.0 + self.tex))
              self.erel = 0.623 * self.evap / self.p * (12.0 - self.vpex) * self.rtv
              self.ere = self.eres + self.erel
              # energy balance
              self.enbal = self.h + self.ed + self.ere + self.esw + self.csum + self.rsum
              if self.count1 == 0:
                  self.xx = 1.0
              if self.count1 == 1:
                  self.xx = 0.1
              if self.count1 == 2:
                  self.xx = 0.01
              if self.count1 == 3:
                  self.xx = 0.001
              if self.enbal > 0.0:
                  self.tx = self.tx - self.xx
              if self.enbal < 0.0:
                  self.tx = self.tx + self.xx
              if (self.enbal > 0.0 or self.enbal2 <= 0.0) and (self.enbal < 0.0 or self.enbal2 >= 0.0):
                  self.enbal2 = self.enbal
              else:
                  self.count1 = self.count1 + 1

          return

      def thermalCategories(self, climate):

          PET = self.tx
          if climate == "humid":
              # categories by Lin and Matzarakis (2008) (tropical and subtropical humid climate)
              if (PET < 14):
                  effectPET = -4
                  comfortablePET = 0
              elif (PET >= 14) and (PET < 18):
                  effectPET = -3
                  comfortablePET = 0
              elif (PET >= 18) and (PET < 22):
                  effectPET = -2
                  comfortablePET = 0
              elif (PET >= 22) and (PET < 26):
                  effectPET = -1
                  comfortablePET = 0
              elif (PET >= 26) and (PET <= 30):
                  effectPET = 0
                  comfortablePET = 1
              elif (PET > 30) and (PET <= 34):
                  effectPET = 1
                  comfortablePET = 0
              elif (PET > 34) and (PET <= 38):
                  effectPET = 2
                  comfortablePET = 0
              elif (PET > 38) and (PET <= 42):
                  effectPET = 3
                  comfortablePET = 0
              elif (PET > 42):
                  effectPET = 4
                  comfortablePET = 0

          elif climate == "temperate":
              # categories by Matzarakis and Mayer (1996) (temperate climate)
              if (PET < 4):
                  effectPET = -4
                  comfortablePET = 0
              elif (PET >= 4) and (PET < 8):
                  effectPET = -3
                  comfortablePET = 0
              elif (PET >= 8) and (PET < 13):
                  effectPET = -2
                  comfortablePET = 0
              elif (PET >= 13) and (PET < 18):
                  effectPET = -1
                  comfortablePET = 0
              elif (PET >= 18) and (PET <= 23):
                  effectPET = 0
                  comfortablePET = 1
              elif (PET > 23) and (PET <= 29):
                  effectPET = 1
                  comfortablePET = 0
              elif (PET > 29) and (PET <= 35):
                  effectPET = 2
                  comfortablePET = 0
              elif (PET > 35) and (PET <= 41):
                  effectPET = 3
                  comfortablePET = 0
              elif (PET > 41):
                  effectPET = 4
                  comfortablePET = 0

          return effectPET, comfortablePET


a = physiologicalEquivalentTemperature(20,20,20,20,2,"m",1,70,"standing",0.5,0.5,"temperate")